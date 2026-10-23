using BepInEx.Configuration;
using EpicLoot.Config;
using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot
{
    public static class EpicLootDropsHelper
    {
        public static bool InstantDropsEnabled { get; set; } = false;

        public static void OnCharacterDeath(CharacterDrop characterDrop)
        {
            if (!TryGetDropInfo(characterDrop, out string characterName, out int level, out Vector3 dropPoint))
            {
                return;
            }

            OnCharacterDeath(characterName, level, dropPoint);
        }

        /// <summary>
        /// Resolves the loot-table name, creature level and drop point for a dying creature. Split out so
        /// callers that need those values for something other than the standard roll (Lucky Loot's bonus
        /// rolls) derive them exactly the same way.
        /// </summary>
        public static bool TryGetDropInfo(CharacterDrop characterDrop, out string characterName,
            out int level, out Vector3 dropPoint)
        {
            characterName = null;
            level = 0;
            dropPoint = Vector3.zero;

            if (characterDrop == null || !CanCharacterDropLoot(characterDrop.m_character))
            {
                return false;
            }

            characterName = EpicLoot.GetCharacterCleanName(characterDrop.m_character);
            level = characterDrop.m_character.GetLevel();
            dropPoint = characterDrop.m_character.GetCenterPoint() +
                characterDrop.transform.TransformVector(characterDrop.m_spawnOffset);
            return true;
        }

        public static bool CanCharacterDropLoot(Character character)
        {
            return character != null && !character.IsTamed();
        }

        public static void OnCharacterDeath(string characterName, int level, Vector3 dropPoint)
        {
            OnCharacterDeath(characterName, level, dropPoint, true);
        }

        /// <param name="allowBonusRolls">False for a roll that is itself a bonus (Lucky Loot's), so
        /// Prosperity's extra rolls apply to the kill once rather than to every bonus roll too.</param>
        public static void OnCharacterDeath(string characterName, int level, Vector3 dropPoint, bool allowBonusRolls)
        {
            List<LootTable> lootTables = LootRoller.GetLootTable(characterName);
            if (lootTables != null && lootTables.Count > 0)
            {
                List<GameObject> loot = LootRoller.RollLootTableAndSpawnObjects(lootTables, level, characterName,
                    dropPoint, allowBonusRolls);
                EpicLoot.Log($"Rolling on loot table: {characterName} (lvl {level}), " +
                    $"spawned {loot.Count} items at drop point({dropPoint}).");
                DropItems(loot, dropPoint);
                // After DropItems, on this machine, which spawned the drops and so owns them.
                Compatibility.ItemFavorites.RequestLootRolls(loot, characterName, dropPoint);
                foreach (GameObject l in loot)
                {
                    ItemDrop.ItemData itemData = l.GetComponent<ItemDrop>().m_itemData;
                    MagicItem magicItem = itemData.GetMagicItem();
                    if (magicItem != null)
                    {
                        EpicLoot.Log($"  - {itemData.m_shared.m_name} <{l.transform.position}>: " +
                            $"{string.Join(", ", magicItem.Effects.Select(x => x.EffectType.ToString()))}");
                    }
                }
            }
            else
            {
                EpicLoot.Log($"Could not find loot table for: {characterName} (lvl {level})");
            }
        }

        public static void DropItems(List<GameObject> loot, Vector3 centerPos, float dropHemisphereRadius = 0.5f)
        {
            foreach (GameObject item in loot)
            {
                Vector3 vector3 = Random.insideUnitSphere * dropHemisphereRadius;
                vector3.y = Mathf.Abs(vector3.y);
                item.transform.position = centerPos + vector3;
                item.transform.rotation = Quaternion.Euler(0.0f, Random.Range(0, 360), 0.0f);

                Rigidbody rigidbody = item.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    Vector3 insideUnitSphere = Random.insideUnitSphere;
                    if (insideUnitSphere.y < 0.0)
                    {
                        insideUnitSphere.y = -insideUnitSphere.y;
                    }
                    rigidbody.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
    public static class CharacterDrop_OnDeath_Patch
    {
        public static void Postfix(CharacterDrop __instance)
        {
            if (!EpicLootDropsHelper.InstantDropsEnabled)
            {
                return;
            }

            EpicLootDropsHelper.OnCharacterDeath(__instance);

            // No-ragdoll path: GenerateDropList ran moments ago in this same frame, so Lucky Loot's latch
            // is consumed directly rather than travelling through a ragdoll ZDO.
            int luckyRolls = LuckyLoot.ConsumePendingRolls(__instance);
            if (luckyRolls > 0 &&
                EpicLootDropsHelper.TryGetDropInfo(__instance, out string name, out int level, out Vector3 dropPoint))
            {
                LuckyLoot.RollBonusEpicLootDrops(name, level, dropPoint, luckyRolls);
            }
        }
    }

    [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.Setup))]
    public static class Ragdoll_Setup_Patch
    {
        public static void Postfix(Ragdoll __instance, CharacterDrop characterDrop)
        {
            if (characterDrop == null || characterDrop.m_character == null || characterDrop.m_character.IsPlayer())
            {
                return;
            }

            if (!EpicLootDropsHelper.CanCharacterDropLoot(characterDrop.m_character))
            {
                return;
            }

            EpicLootDropsHelper.InstantDropsEnabled = false;

            string characterName = EpicLoot.GetCharacterCleanName(characterDrop.m_character);
            int level = characterDrop.m_character.GetLevel();
            __instance.m_nview.m_zdo.Set("characterName", characterName);
            __instance.m_nview.m_zdo.Set("level", level);

            // Ragdoll.Setup has just called SaveLootList -> CharacterDrop.GenerateDropList, so if Lucky
            // Loot procced its latch is still warm and keyed to this very CharacterDrop. Carry the bonus
            // roll count on the ragdoll's ZDO, since the magic-item half doesn't happen until SpawnLoot,
            // possibly minutes later. Only written when non-zero, to keep the key off every ragdoll in the
            // world. We just instantiated this ragdoll, so the write is on a ZDO we own.
            int luckyRolls = LuckyLoot.ConsumePendingRolls(characterDrop);
            if (luckyRolls > 0)
            {
                __instance.m_nview.m_zdo.Set(LuckyLoot.ZdoBonusRollsKey, luckyRolls);
            }
        }
    }

    [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.SpawnLoot))]
    public static class Ragdoll_SpawnLoot_Patch
    {
        public static void Postfix(Ragdoll __instance, Vector3 center)
        {
            string characterName = __instance.m_nview.m_zdo.GetString("characterName");
            int level = __instance.m_nview.m_zdo.GetInt("level");

            if (!string.IsNullOrEmpty(characterName))
            {
                Vector3 dropPoint = center + Vector3.up * 0.75f;
                EpicLootDropsHelper.OnCharacterDeath(characterName, level, dropPoint);

                // A Harmony postfix still runs when vanilla SpawnLoot early-returns on s_drops <= 0, so a
                // creature whose vanilla drop list was empty still gets its Lucky Loot bonus rolls.
                LuckyLoot.RollBonusEpicLootDrops(characterName, level, dropPoint,
                    __instance.m_nview.m_zdo.GetInt(LuckyLoot.ZdoBonusRollsKey));
            }
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDrop_GenerateDropList_DropsEnabled
    {
        [HarmonyPriority(Priority.First)]
        [HarmonyBefore(new [] { "org.bepinex.plugins.creaturelevelcontrol" })]
        public static void Postfix(CharacterDrop __instance)
        {
            EpicLootDropsHelper.InstantDropsEnabled = __instance.m_dropsEnabled;
        }
    }

    // Boss per-player drops. Under the OnePerPlayer modes a boss's trophy, Wishbone and CryptKey each
    // gain one extra copy for every counted player after the first. The late bosses' own drops (Dragon
    // Tears, Torn Spirit, Majestic Carapace, Fader Relic, Sacrificial Blood) follow their boss's
    // BossExtraDropMode instead: one extra per nearby player, or the whole drop once per nearby player.
    // A mode only ever adds to the amount the list already carries (vanilla's roll plus whatever a mod
    // ahead of us made of it), so it never lowers, replaces or resurrects a drop, and a kill with no one
    // counted still drops the normal amount.
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDrop_GenerateDropList_Patch
    {
        // Vanilla's m_onePerPlayer (Wishbone, CryptKey) replaces the drop's amount with the whole server's
        // player count, which would leave nothing for the mode to add to. Where a mode owns such a drop the
        // flag is suspended for this call only, so vanilla generates the drop's own amount; the postfix puts
        // it back before any later postfix (Lucky Loot's boss-reward guard) reads it.
        public static void Prefix(CharacterDrop __instance, out List<CharacterDrop.Drop> __state)
        {
            __state = null;
            if (!IsBoss(__instance) || __instance.m_drops == null)
            {
                return;
            }

            foreach (CharacterDrop.Drop drop in __instance.m_drops)
            {
                if (drop != null && drop.m_onePerPlayer && drop.m_prefab != null &&
                    GetGrowth(drop.m_prefab, out _, out _) != Growth.None)
                {
                    drop.m_onePerPlayer = false;
                    (__state ??= new List<CharacterDrop.Drop>()).Add(drop);
                }
            }
        }

        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result,
            List<CharacterDrop.Drop> __state)
        {
            RestoreOnePerPlayer(__state);

            if (__result == null || !IsBoss(__instance))
            {
                return;
            }

            Vector3 position = __instance.m_character.transform.position;
            // Once per prefab, so a drop table listing the same trophy twice doesn't pay out twice per player.
            HashSet<GameObject> extended = new HashSet<GameObject>();
            for (int index = 0; index < __result.Count; index++)
            {
                GameObject prefab = __result[index].Key;
                int amount = __result[index].Value;
                // An amount of zero is another mod suppressing the drop; leave it suppressed.
                if (prefab == null || amount <= 0 || extended.Contains(prefab))
                {
                    continue;
                }

                Growth growth = GetGrowth(prefab, out bool wholeServer, out float range);
                if (growth == Growth.None)
                {
                    continue;
                }

                extended.Add(prefab);
                int players = CountPlayers(wholeServer, position, range);
                if (players <= 1)
                {
                    continue;
                }

                int total = growth == Growth.Multiply ? amount * players : amount + players - 1;
                EpicLoot.Log($"Boss drop {prefab.name}: {amount} -> {total} for {players} players ({growth})");
                __result[index] = new KeyValuePair<GameObject, int>(prefab, total);
            }
        }

        private enum Growth
        {
            None,
            OneExtraEach,
            Multiply
        }

        // Only has work left to do when GenerateDropList threw, since the postfix empties the list.
        public static void Finalizer(List<CharacterDrop.Drop> __state)
        {
            RestoreOnePerPlayer(__state);
        }

        private static void RestoreOnePerPlayer(List<CharacterDrop.Drop> suspended)
        {
            if (suspended == null)
            {
                return;
            }

            foreach (CharacterDrop.Drop drop in suspended)
            {
                drop.m_onePerPlayer = true;
            }
            suspended.Clear();
        }

        private static bool IsBoss(CharacterDrop characterDrop)
        {
            return characterDrop != null && characterDrop.m_character != null && characterDrop.m_character.IsBoss();
        }

        // Wishbone, CryptKey and the late bosses' own drops follow their own settings; every other trophy
        // follows the trophy settings.
        private static Growth GetGrowth(GameObject prefab, out bool wholeServer, out float range)
        {
            switch (prefab.name)
            {
                case "Wishbone":
                    return FromMode(EpicLoot.GetBossWishboneDropMode(), EpicLoot.GetBossWishboneDropPlayerRange(), out wholeServer, out range);
                case "CryptKey":
                    return FromMode(EpicLoot.GetBossCryptKeyDropMode(), EpicLoot.GetBossCryptKeyPlayerRange(), out wholeServer, out range);
                case "DragonTear":
                    return FromMode(ELConfig.ModerDropMode, ELConfig.ModerDropPlayerRange, out wholeServer, out range);
                case "YagluthDrop":
                    return FromMode(ELConfig.YagluthDropMode, ELConfig.YagluthDropPlayerRange, out wholeServer, out range);
                case "QueenDrop":
                    return FromMode(ELConfig.QueenDropMode, ELConfig.QueenDropPlayerRange, out wholeServer, out range);
                case "FaderDrop":
                    return FromMode(ELConfig.FaderDropMode, ELConfig.FaderDropPlayerRange, out wholeServer, out range);
                case "FrozenKingDrop":
                    return FromMode(ELConfig.FrozenKingDropMode, ELConfig.FrozenKingDropPlayerRange, out wholeServer, out range);
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop?.m_itemData?.m_shared != null &&
                itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
            {
                return FromMode(EpicLoot.GetBossTrophyDropMode(), EpicLoot.GetBossTrophyDropPlayerRange(), out wholeServer, out range);
            }

            wholeServer = false;
            range = 0f;
            return Growth.None;
        }

        private static Growth FromMode(BossDropMode mode, float playerRange, out bool wholeServer, out float range)
        {
            wholeServer = mode == BossDropMode.OnePerPlayerOnServer;
            range = playerRange;
            return mode == BossDropMode.Default ? Growth.None : Growth.OneExtraEach;
        }

        private static Growth FromMode(ConfigEntry<BossExtraDropMode> mode, ConfigEntry<float> playerRange,
            out bool wholeServer, out float range)
        {
            wholeServer = false;
            range = playerRange?.Value ?? 0f;
            switch (mode?.Value ?? BossExtraDropMode.Default)
            {
                case BossExtraDropMode.OneExtraPerNearbyPlayer:
                    return Growth.OneExtraEach;
                case BossExtraDropMode.MultiplyByNearbyPlayers:
                    return Growth.Multiply;
                default:
                    return Growth.None;
            }
        }

        private static int CountPlayers(bool wholeServer, Vector3 position, float range)
        {
            List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
            if (wholeServer)
            {
                return playerList.Count;
            }

            // Characters this machine has loaded, or the server's list for anyone it hasn't. A player
            // hiding their map position is listed at the world origin, so never counts as near.
            return Math.Max(Player.GetPlayersInRangeXZ(position, range),
                playerList.Count(x => x.m_publicPosition && Vector3.Distance(x.m_position, position) <= range));
        }
    }
}
