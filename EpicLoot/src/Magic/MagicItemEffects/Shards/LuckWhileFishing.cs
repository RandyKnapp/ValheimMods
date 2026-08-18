using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Provides a chance to increase the number of fish caught and to pull up bonus treasure while fishing
    //
    // Hooked on FishingFloat.Catch rather than Fish.Pickup: Fish.Pickup returns true as soon as it has
    // *fired* the RequestPickup RPC, and RPC_RequestPickup drops requests within 2s of each other, so a
    // Pickup postfix can pay out for a catch the player never received. Catch also only runs for actual
    // rod fishing (from the float owner's FixedUpdate, with the rod owner passed in), which is what this
    // effect is meant to reward. Everything below is a local inventory operation on the fisher's own
    // client, so there is no ownership question.
    //
    // Shard values are authored as whole-number percents, hence the 0.01f.
    public static class LuckWhileFishing
    {
        // Config keys that are tunables rather than loot prefabs. Matched explicitly so a future tunable
        // that happens to collide with a prefab name can never be silently rolled as treasure.
        public const string TripleChanceKey = "TripleChance";

        private static readonly HashSet<string> ReservedConfigKeys = new HashSet<string> { TripleChanceKey };

        // Percent of successful multi-catch procs that yield +2 fish instead of +1.
        public const float DefaultTripleChance = 20f;

        // The treasure roll. A single roll is made and the highest-value entry it can afford is taken --
        // the same "value threshold" model Riches uses, where the config number is what the roll has to
        // reach rather than a selection weight.
        //   rollMax = RollBase + value * RollPerValue   the range the roll can reach at all
        //   roll    = pow(random, RollCurve) * rollMax  biased low, so each higher tier is rarer
        //   floor   = rollMax * FloorFraction           climbs with rollMax, dropping cheap entries out
        // Kept as consts rather than Config keys: every Config key renders as its own line in the
        // detailed (Shift) tooltip, and the prefab list already fills it.
        private const float RollBase = 40f;
        private const float RollPerValue = 12f;
        private const float RollCurve = 2f;
        private const float FloorFraction = 0.05f;

        // Default Config block, registered in ShardEffectDefinitions.EffectConfigs. Prefab name -> value
        // threshold, mirroring the Riches entry in magiceffects.json. An admin overrides the whole table
        // by adding a "Type": "LuckWhileFishing" entry with its own Config to magiceffects.json.
        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float>
        {
            { TripleChanceKey, DefaultTripleChance },
            { "Flint", 5 },
            { "Coins", 15 },
            { "Amber", 40 },
            { "Ruby", 80 },
            { "Chitin", 100 },
            { "AmberPearl", 120 },
            { "CopperScrap", 180 },
            { "SilverNecklace", 240 },
            { "IronScrap", 300 },
            { "SerpentScale", 360 },
            { "RunestoneEpic", 500 },
            { "RunestoneLegendary", 640 },
        };

        // How many units each treasure entry grants. Prefabs present in Config but absent here grant a
        // single unit, so an admin can add any prefab to the table and it just works.
        private static readonly Dictionary<string, Vector2Int> AmountsByPrefab = new Dictionary<string, Vector2Int>
        {
            { "Flint", new Vector2Int(1, 3) },
            { "Coins", new Vector2Int(5, 50) },
            { "AmberPearl", new Vector2Int(1, 2) },
            { "SerpentScale", new Vector2Int(1, 3) },
        };

        private static readonly Vector2Int SingleUnit = new Vector2Int(1, 1);

        // Tooltip: "... {0}% ... ({1}% of those ...)" -- {1} surfaces the triple-catch sub-roll.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.LuckWhileFishing,
                value => new object[] { value, GetTripleChance() });
        }

        [HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
        private static class FishingFloat_Catch_Patch
        {
            // Catch builds the "$msg_fishing_catched <fish> & <extra>" string the float shows the player,
            // so bonuses are appended to __result in the same style vanilla uses for Fish.m_extraDrops.
            [UsedImplicitly]
            private static void Postfix(Fish fish, Character owner, ref string __result)
            {
                var player = Player.m_localPlayer;
                if (fish == null || player == null || owner != player || ObjectDB.instance == null)
                {
                    return;
                }

                var chance = player.GetTotalActiveMagicEffectValue(MagicEffectType.LuckWhileFishing, 0.01f);
                if (chance <= 0f)
                {
                    return;
                }

                // Roll A -- bonus treasure.
                if (Random.value < chance && TryRollTreasure(chance * 100f, out var prefabName, out var amount))
                {
                    __result = GrantTreasure(player, fish, prefabName, amount, __result);
                }

                // Roll B -- multi-catch.
                if (Random.value < chance)
                {
                    var extra = Random.Range(0f, 100f) < GetTripleChance() ? 2 : 1;
                    __result = GrantExtraFish(player, fish, extra, __result);
                }
            }
        }

        // Rolls the treasure table for a given effect value. Public for the "fishlucktest" terminal
        // command, which samples it to verify the curve without grinding catches.
        public static bool TryRollTreasure(float value, out string prefabName, out int amount)
        {
            prefabName = null;
            amount = 0;

            var table = ResolveTable();
            if (table.Count == 0)
            {
                return false;
            }

            var rollMax = RollBase + value * RollPerValue;
            var roll = Mathf.Pow(Random.value, RollCurve) * rollMax;
            var floor = rollMax * FloorFraction;

            // Table is ascending by threshold: walk up keeping the last affordable entry that the floor
            // still allows, and remember the cheapest allowed entry as the fallback.
            var selected = -1;
            var cheapestAllowed = -1;
            for (var i = 0; i < table.Count; i++)
            {
                var threshold = table[i].Value;
                if (threshold < floor)
                {
                    continue; // dropped out of the table entirely at this effect value
                }

                if (cheapestAllowed < 0)
                {
                    cheapestAllowed = i;
                }

                if (threshold > roll)
                {
                    break; // ascending, so nothing beyond this is affordable either
                }

                selected = i;
            }

            // A roll short of every allowed entry still pays out the cheapest one, so a successful proc
            // never grants nothing -- the same lowest-cost fallback Riches uses.
            if (selected < 0)
            {
                selected = cheapestAllowed;
            }

            if (selected < 0)
            {
                return false; // the floor has risen above every entry in the table
            }

            prefabName = table[selected].Key;
            var range = AmountsByPrefab.TryGetValue(prefabName, out var r) ? r : SingleUnit;
            amount = Random.Range(range.x, range.y + 1);
            return true;
        }

        // The configured treasure entries that resolve to a real item prefab, ascending by threshold.
        // Rebuilt per proc rather than cached: procs are rare and the table is tiny, so this stays clear
        // of the ObjectDB-timing and stale-cache handling Riches needs for its per-kill drop path.
        public static List<KeyValuePair<string, float>> ResolveTable()
        {
            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.LuckWhileFishing);
            if (config == null || config.Count == 0)
            {
                config = DefaultConfig;
            }

            var table = new List<KeyValuePair<string, float>>();
            foreach (var entry in config)
            {
                if (ReservedConfigKeys.Contains(entry.Key) || entry.Value <= 0f)
                {
                    continue;
                }

                if (ObjectDB.instance == null || !ObjectDB.instance.TryGetItemPrefab(entry.Key, out _))
                {
                    continue; // unresolvable prefab names are skipped, matching Riches
                }

                table.Add(entry);
            }

            table.Sort((a, b) => a.Value.CompareTo(b.Value));
            return table;
        }

        public static float GetTripleChance()
        {
            var config = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.LuckWhileFishing);
            if (config != null && config.TryGetValue(TripleChanceKey, out var raw))
            {
                return Mathf.Clamp(raw, 0f, 100f);
            }

            return DefaultTripleChance;
        }

        private static string GrantTreasure(Player player, Fish fish, string prefabName, int amount, string message)
        {
            if (!ObjectDB.instance.TryGetItemPrefab(prefabName, out var prefab)
                || !prefab.TryGetComponent(out ItemDrop itemDrop))
            {
                return message;
            }

            AddOrSpill(player, fish.transform.position, prefab, prefabName, amount, quality: 1, variant: 0);
            return $"{message} & {amount}x {itemDrop.m_itemData.m_shared.m_name}";
        }

        // Grants extra copies of the fish just caught, matching its size (fish carry their size in
        // m_quality -- see Fish.Awake) so a bonus catch is worth the same as the real one.
        private static string GrantExtraFish(Player player, Fish fish, int extra, string message)
        {
            if (!fish.TryGetComponent(out ItemDrop itemDrop))
            {
                return message; // nothing to duplicate
            }

            var item = itemDrop.m_itemData;
            var prefab = item.m_dropPrefab;
            if (prefab == null)
            {
                return message;
            }

            AddOrSpill(player, fish.transform.position, prefab, Utils.GetPrefabName(prefab), extra,
                item.m_quality, item.m_variant);
            return $"{message} & {extra}x {item.m_shared.m_name}";
        }

        // Adds units one at a time so a partially-full inventory still takes what it can, then spills the
        // remainder on the ground -- the same no-room handling vanilla uses for Fish.m_extraDrops in
        // FishingFloat.Catch.
        private static void AddOrSpill(Player player, Vector3 position, GameObject prefab, string prefabName,
            int amount, int quality, int variant)
        {
            var remaining = amount;
            bool _full = false;

            // AddItem returns the added item on success and null on failure (full inventory, missing
            // prefab), so keep adding while it succeeds and spill whatever is left when it stops.
            while (remaining > 0
                && player.GetInventory().AddItem(prefabName, 1, quality, variant, 0L, "", pickedUp: true) != null)
            {
                remaining--;
            }

            if (remaining <= 0)
            {
                return;
            }

            _full = true;

            while (remaining > 0)
            {
                var rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
                var spawned = Object.Instantiate(prefab, position + Vector3.up * 0.5f, rotation);
                if (!spawned.TryGetComponent(out ItemDrop spilled))
                {
                    Object.Destroy(spawned);
                    return;
                }

                ItemDrop.OnCreateNew(spilled);
                spilled.SetQuality(quality);
                spilled.m_itemData.m_variant = variant;

                // SetStack clamps to the prefab's max stack size, so loop until everything is placed.
                // Max(1) keeps a misconfigured prefab from spinning here forever.
                var placed = Mathf.Max(1, Mathf.Min(remaining, spilled.m_itemData.m_shared.m_maxStackSize));
                spilled.SetStack(placed);
                remaining -= placed;
            }

            if (_full) {
                player.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$inventory_full"));
            }
        }
    }
}
