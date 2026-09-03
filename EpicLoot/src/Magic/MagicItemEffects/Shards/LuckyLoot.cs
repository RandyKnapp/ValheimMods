using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot.MagicItemEffects.Shards {
    // Golden chest: a chance for a creature killed nearby to drop a multiple of its usual loot, and to
    // roll extra times on its EpicLoot magic-item table.
    //
    // Two loot systems have to be hit, and they do not run at the same time:
    //
    //   Character.OnDeath  (runs on the CREATURE'S OWNER, which may be a dedicated server)
    //     +- Ragdoll.Setup
    //     |    +- SaveLootList -> CharacterDrop.GenerateDropList()   <-- the proc is rolled here, and the
    //     |                                                              vanilla drop list multiplied
    //     +- Ragdoll_Setup_Patch.Postfix                             <-- the bonus-roll count is written
    //     |                                                              to the ragdoll's ZDO
    //     +- SetDropsEnabled(false)   // so CharacterDrop.OnDeath won't generate a second list
    //          ... seconds to minutes later, when the ragdoll is removed ...
    //   Ragdoll.SpawnLoot                                            <-- the ZDO is read back and the
    //                                                                    magic-item table re-rolled
    //
    // GenerateDropList is the only place the vanilla list can be touched, and thanks to the
    // SetDropsEnabled interlock it runs exactly once per death. The EpicLoot half has to happen where
    // EpicLootDropsHelper.OnCharacterDeath already runs, which is a different moment and possibly a
    // different machine -- hence the ragdoll ZDO, with a short-lived instance-keyed latch bridging
    // GenerateDropList to Ragdoll.Setup. On the no-ragdoll path (InstantDropsEnabled) both halves land in
    // the same frame and the latch is consumed directly.
    //
    // Known limitation: a creature with no CharacterDrop, or with m_dropItems off, never runs
    // GenerateDropList and so can never proc Lucky Loot -- including the magic-item half.
    public static class LuckyLoot {
        public const float DefaultBonusRollsMin = 1f;
        public const float DefaultBonusRollsMax = 6f;

        // Ceiling on the derived multiplier, so an admin who sets the chance to 50% doesn't get a 26x
        // drop. The shipped ramp (2/4/6/8/10) tops out at 6x, well inside this.
        public const int DefaultMaxMultiplier = 10;
        // Vanilla's own per-entry cap (CharacterDrop.GenerateDropList clamps to this before we run).
        // CharacterDrop.DropItems instantiates one networked GameObject *per unit* -- it does not stack --
        // so multiplying past vanilla's own ceiling is how one kill turns into four figures of ItemDrops.
        // Raising it is the fastest way to make a single kill spawn enough objects to stall the server.
        public const int DefaultMaxUnitsPerEntry = 100;
        // Belt and braces for creatures (or mods) with many drop entries.
        public const int DefaultMaxAddedUnitsPerDeath = 300;

        private const string BonusRollsMinKey = "BonusRollsMin";
        private const string BonusRollsMaxKey = "BonusRollsMax";
        private const string MaxMultiplierKey = "MaxMultiplier";
        private const string MaxUnitsPerEntryKey = "MaxUnitsPerEntry";
        private const string MaxAddedUnitsPerDeathKey = "MaxAddedUnitsPerDeath";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { BonusRollsMinKey, DefaultBonusRollsMin },
            { BonusRollsMaxKey, DefaultBonusRollsMax },
            { MaxMultiplierKey, DefaultMaxMultiplier },
            { MaxUnitsPerEntryKey, DefaultMaxUnitsPerEntry },
            { MaxAddedUnitsPerDeathKey, DefaultMaxAddedUnitsPerDeath },
        };

        // All three floored at 1: a cap of zero would either delete the drop list or make the proc inert,
        // neither of which is a sane tuning outcome.
        private static int GetMaxMultiplier() {
            return Mathf.Max(1, Mathf.RoundToInt(GetConfigValue(MaxMultiplierKey, DefaultMaxMultiplier)));
        }

        private static int GetMaxUnitsPerEntry() {
            return Mathf.Max(1, Mathf.RoundToInt(GetConfigValue(MaxUnitsPerEntryKey, DefaultMaxUnitsPerEntry)));
        }

        private static int GetMaxAddedUnitsPerDeath() {
            return Mathf.Max(1,
                Mathf.RoundToInt(GetConfigValue(MaxAddedUnitsPerDeathKey, DefaultMaxAddedUnitsPerDeath)));
        }

        // Matches Riches. The shard owner has to be in the area for the kill to count.
        private const float PlayerScanRange = 100f;
        // The latch only has to survive GenerateDropList -> Ragdoll.Setup, which is the same call stack.
        private const float LatchLifetimeSeconds = 5f;

        // Player-ZDO key holding the local player's Lucky Loot value, mirrored by
        // Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip.
        public const string ZdoValueKey = "el-lky";
        // Ragdoll-ZDO key holding the number of bonus EpicLoot rolls a procced kill earned.
        public const string ZdoBonusRollsKey = "el-lky-rolls";

        private static CharacterDrop _pendingOwner;
        private static int _pendingRolls;
        private static float _pendingStamp;

        // Tooltip: "{0}% Chance of {1}x Drops" plus the bonus-roll range. Pure, as the provider contract
        // requires (MagicItem.RegisterDisplayValues): it only reads the value and the effect config.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.LuckyLoot,
                value => new object[] { value, (float)Multiplier(value), GetBonusRollsMin(), GetBonusRollsMax() });
        }

        // The multiplier is derived from the single grid value rather than authored separately: 2% -> 2x,
        // 4% -> 3x, ... 10% -> 6x. A parallel MultiplierPerRarity block would be a second source of truth
        // that silently disagrees with the grid the moment an admin edits ValuesPerRarity.
        //
        // The lower clamp is 1, not 2, on purpose: MagicItem.GetGenericArgs probes display-value providers
        // with 1f and 2f and treats any slot that doesn't change as a constant, so a floor of 2 would
        // freeze {1} in the generic tooltip.
        private static int Multiplier(float value) {
            return Mathf.Clamp(1 + Mathf.RoundToInt(value * 0.5f), 1, GetMaxMultiplier());
        }

        // The creature's owner runs this, and on a dedicated server that machine has no local player at
        // all -- so, like Riches, the value is read off nearby players' ZDOs rather than from
        // Player.m_localPlayer. Writing this the way Headhunter does would silently disable the effect in
        // the most common multiplayer topology.
        //
        // Unlike Riches this takes the max rather than the sum, and does not cache. Max, because the value
        // is simultaneously a chance and (through Multiplier) a drop multiple -- summing two players' 10%
        // into 20% would imply an 11x multiplier rather than 6x. No cache, because Riches' 5-second one is
        // a single scalar shared across every creature in the world, and a player scan on death is cheap
        // when deaths are rare relative to frames.
        private static float BestNearbyValue(Vector3 position) {
            var players = new List<Player>();
            Player.GetPlayersInRange(position, PlayerScanRange, players);

            var best = 0f;
            foreach (var player in players) {
                var zdo = player?.m_nview?.GetZDO();
                if (zdo == null) {
                    continue;
                }

                var value = zdo.GetFloat(ZdoValueKey);
                if (value > best) {
                    best = value;
                }
            }
            return best;
        }

        [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
        public static class LuckyLoot_CharacterDrop_GenerateDropList_Patch {
            // Last, so the list we multiply is the final one -- after EpicLoot's own boss-trophy rewrite
            // and after CreatureLevelControl.
            [HarmonyPriority(Priority.Last)]
            [UsedImplicitly]
            private static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result) {
                ClearStaleLatch();

                if (__instance == null || __result == null || __instance.m_character == null ||
                    __instance.m_character.IsTamed()) {
                    return;
                }

                var value = BestNearbyValue(__instance.m_character.transform.position);
                if (value <= 0 || Random.value >= value * 0.01f) {
                    return;
                }

                var multiplier = Multiplier(value);
                MultiplyDropList(__instance, __result, multiplier);

                _pendingOwner = __instance;
                _pendingRolls = Random.Range(Mathf.RoundToInt(GetBonusRollsMin()),
                    Mathf.RoundToInt(GetBonusRollsMax()) + 1);
                _pendingStamp = Time.time;

                EpicLoot.Log($"Lucky Loot proc on {__instance.m_character.name}: {multiplier}x drops, " +
                    $"{_pendingRolls} bonus magic item rolls (value {value}).");
            }
        }

        private static void MultiplyDropList(CharacterDrop characterDrop,
            List<KeyValuePair<GameObject, int>> dropList, int multiplier) {
            if (multiplier <= 1) {
                return;
            }

            // m_onePerPlayer is read off m_drops because __result has already collapsed it into a plain
            // amount. This is the primary boss-reward guard.
            var excluded = new HashSet<GameObject>();
            foreach (var drop in characterDrop.m_drops) {
                if (drop != null && drop.m_onePerPlayer && drop.m_prefab != null) {
                    excluded.Add(drop.m_prefab);
                }
            }

            var added = 0;
            for (var i = 0; i < dropList.Count; i++) {
                if (added >= GetMaxAddedUnitsPerDeath()) {
                    break;
                }

                var prefab = dropList[i].Key;
                if (prefab == null || excluded.Contains(prefab) || IsProtected(prefab)) {
                    continue;
                }

                var amount = dropList[i].Value;
                var newAmount = Mathf.Min(amount * multiplier, GetMaxUnitsPerEntry());
                if (newAmount > amount) {
                    added += newAmount - amount;
                    dropList[i] = new KeyValuePair<GameObject, int>(prefab, newAmount);
                }
            }
        }

        // Trophies and the two named boss keys are never multiplied.
        //
        // The name check is not redundant with the m_onePerPlayer guard: EpicLoot's own
        // CharacterDrop_GenerateDropList_Patch.Prefix *clears* m_onePerPlayer on Wishbone and CryptKey
        // under the non-default boss drop modes, so that guard is defeatable by config. Excluding all
        // trophies on top of that keeps Lucky Loot out of Headhunter's lane (DarkRed head) and draws a
        // legible line: Lucky Loot multiplies materials, not trophies.
        private static bool IsProtected(GameObject prefab) {
            if (prefab.name.Equals("Wishbone") || prefab.name.Equals("CryptKey")) {
                return true;
            }

            var itemDrop = prefab.GetComponent<ItemDrop>();
            return itemDrop?.m_itemData?.m_shared != null &&
                itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }

        // Keyed on the CharacterDrop instance, not just "the last proc", so a stale value can never bleed
        // onto the next creature. Ragdoll.Setup receives the very same CharacterDrop it passed to
        // SaveLootList, so the match is exact.
        public static int ConsumePendingRolls(CharacterDrop drop) {
            ClearStaleLatch();
            if (drop == null || _pendingOwner != drop) {
                return 0;
            }

            var rolls = _pendingRolls;
            _pendingOwner = null;
            _pendingRolls = 0;
            return rolls;
        }

        private static void ClearStaleLatch() {
            if (_pendingOwner != null && Time.time - _pendingStamp > LatchLifetimeSeconds) {
                _pendingOwner = null;
                _pendingRolls = 0;
            }
        }

        // Re-rolls the creature's EpicLoot magic-item table extraRolls more times. Normal rarity and
        // gating rules apply to each roll, so a roll legitimately producing nothing is expected.
        public static void RollBonusEpicLootDrops(string characterName, int level, Vector3 dropPoint,
            int extraRolls) {
            if (extraRolls <= 0 || string.IsNullOrEmpty(characterName)) {
                return;
            }

            EpicLoot.Log($"Lucky Loot: rolling {extraRolls} bonus magic item drops for {characterName}.");
            for (var i = 0; i < extraRolls; i++) {
                EpicLootDropsHelper.OnCharacterDeath(characterName, level, dropPoint);
            }
        }

        private static float GetBonusRollsMin() {
            return GetConfigValue(BonusRollsMinKey, DefaultBonusRollsMin);
        }

        private static float GetBonusRollsMax() {
            return Mathf.Max(GetBonusRollsMin(), GetConfigValue(BonusRollsMaxKey, DefaultBonusRollsMax));
        }

        private static float GetConfigValue(string key, float fallback) {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.LuckyLoot);
            if (cfg != null && cfg.TryGetValue(key, out var raw)) {
                return Mathf.Max(0f, raw);
            }
            return fallback;
        }
    }
}
