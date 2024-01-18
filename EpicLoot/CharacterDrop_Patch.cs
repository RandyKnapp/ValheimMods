using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static class EpicLootDropsHelper
    {
        public static bool InstantDropsEnabled { get; set; } = false;
    }

    //public void OnDeath()
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.OnDeath))]
    public static class CharacterDrop_OnDeath_Patch
    {
        public static void Postfix(CharacterDrop __instance)
        {
            if (EpicLootDropsHelper.InstantDropsEnabled)
            {
                EpicLoot.OnCharacterDeath(__instance);
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

            if (!EpicLoot.CanCharacterDropLoot(characterDrop.m_character))
            {
                return;
            }

            EpicLootDropsHelper.InstantDropsEnabled = false;

            var characterName = EpicLoot.GetCharacterCleanName(characterDrop.m_character);
            var level = characterDrop.m_character.GetLevel();
            __instance.m_nview.m_zdo.Set("characterName", characterName);
            __instance.m_nview.m_zdo.Set("level", level);
        }
    }

    [HarmonyPatch(typeof(Ragdoll), nameof(Ragdoll.SpawnLoot))]
    public static class Ragdoll_SpawnLoot_Patch
    {
        public static void Postfix(Ragdoll __instance, Vector3 center)
        {
            var characterName = __instance.m_nview.m_zdo.GetString("characterName");
            var level = __instance.m_nview.m_zdo.GetInt("level");

            if (!string.IsNullOrEmpty(characterName))
            {
                EpicLoot.OnCharacterDeath(characterName, level, center + Vector3.up * 0.75f);
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
        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            if (__instance.m_character != null && __instance.m_character.IsBoss() && EpicLoot.GetBossTrophyDropMode() != BossDropMode.Default)
            {
                for (var index = 0; index < __result.Count; index++)
                {
                    var entry = __result[index];
                    var prefab = entry.Key;

                    var itemDrop = prefab.GetComponent<ItemDrop>();

                    if (itemDrop == null || itemDrop.m_itemData == null)
                    {
                        continue;
                    }

                    if (itemDrop.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
                    {
                        int dropCount;
                        var playerList = ZNet.instance.GetPlayerList();
                        switch (EpicLoot.GetBossTrophyDropMode())
                        {
                            case BossDropMode.OnePerPlayerOnServer:
                                dropCount = playerList.Count;
                                break;

                            case BossDropMode.OnePerPlayerNearBoss:
                                dropCount = Math.Max(Player.GetPlayersInRangeXZ(__instance.m_character.transform.position,  EpicLoot.GetBossTrophyDropPlayerRange()), playerList.Count(x => Vector3.Distance(x.m_position, __instance.m_character.transform.position) <= EpicLoot.GetBossTrophyDropPlayerRange()));
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
