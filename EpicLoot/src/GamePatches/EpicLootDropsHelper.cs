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
            if (!CanCharacterDropLoot(characterDrop.m_character))
            {
                return;
            }

            string characterName = EpicLoot.GetCharacterCleanName(characterDrop.m_character);
            int level = characterDrop.m_character.GetLevel();
            Vector3 dropPoint = characterDrop.m_character.GetCenterPoint() +
                characterDrop.transform.TransformVector(characterDrop.m_spawnOffset);

            OnCharacterDeath(characterName, level, dropPoint);
        }

        public static bool CanCharacterDropLoot(Character character)
        {
            return character != null && !character.IsTamed();
        }

        public static void OnCharacterDeath(string characterName, int level, Vector3 dropPoint)
        {
            List<LootTable> lootTables = LootRoller.GetLootTable(characterName);
            if (lootTables != null && lootTables.Count > 0)
            {
                List<GameObject> loot = LootRoller.RollLootTableAndSpawnObjects(lootTables, level, characterName, dropPoint);
                EpicLoot.Log($"Rolling on loot table: {characterName} (lvl {level}), " +
                    $"spawned {loot.Count} items at drop point({dropPoint}).");
                DropItems(loot, dropPoint);
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
            if (EpicLootDropsHelper.InstantDropsEnabled)
            {
                EpicLootDropsHelper.OnCharacterDeath(__instance);
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
                EpicLootDropsHelper.OnCharacterDeath(characterName, level, center + Vector3.up * 0.75f);
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

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDrop_GenerateDropList_Patch
    {
        public static void Prefix(CharacterDrop __instance)
        {
            if (__instance.m_character != null && __instance.m_character.IsBoss() &&
                EpicLoot.GetBossTrophyDropMode() != BossDropMode.Default)
            {
                foreach (CharacterDrop.Drop drop in __instance.m_drops)
                {
                    if (!(drop.m_prefab == null))
                    {
                        if ((drop.m_prefab.name.Equals("Wishbone") && EpicLoot.GetBossWishboneDropMode() != BossDropMode.Default) ||
                            (drop.m_prefab.name.Equals("CryptKey") && EpicLoot.GetBossCryptKeyDropMode() != BossDropMode.Default))
                            if (drop.m_onePerPlayer)
                                drop.m_onePerPlayer = false;
                    }
                }
            }
        }

        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (__instance.m_character != null && __instance.m_character.IsBoss() && EpicLoot.GetBossTrophyDropMode() != BossDropMode.Default)
            {
                for (int index = 0; index < __result.Count; index++)
                {
                    KeyValuePair<GameObject, int> entry = __result[index];
                    GameObject prefab = entry.Key;

                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

                    if (itemDrop == null || itemDrop.m_itemData == null)
                    {
                        continue;
                    }

                    if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy ||
                        prefab.name.Equals("Wishbone") ||
                        prefab.name.Equals("CryptKey"))
                    {
                        int dropCount;
                        List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
                        switch (EpicLoot.GetBossTrophyDropMode())
                        {
                            case BossDropMode.OnePerPlayerOnServer:
                                dropCount = playerList.Count;
                                break;
                            case BossDropMode.OnePerPlayerNearBoss:
                                Vector3 position = __instance.m_character.transform.position;
                                float range = EpicLoot.GetBossTrophyDropPlayerRange();
                                dropCount = Math.Max(Player.GetPlayersInRangeXZ(position, range),
                                    playerList.Count(x => Vector3.Distance(x.m_position, position) <= range));
                                break;
                            default:
                                dropCount = 1;
                                break;
                        }

                        EpicLoot.Log($"Dropping trophies: {dropCount} (mode={EpicLoot.GetBossTrophyDropMode()})");
                        __result[index] = new KeyValuePair<GameObject, int>(prefab, dropCount);
                    }
                }
            }
        }
    }
}
