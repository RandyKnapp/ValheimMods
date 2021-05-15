using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    //public List<KeyValuePair<GameObject, int>> GenerateDropList()
    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    public static class CharacterDrop_GenerateDropList_Patch
    {
        public static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
        {
            for (var index = 0; index < __result.Count; index++)
            {
                var dropEntry = __result[index];
                if (dropEntry.Key.name == "Coins")
                {
                    __result.RemoveAt(index);
                    var newAmount = dropEntry.Value * AdventureDataManager.Config.FulingCoinDropScale;
                    __result.Insert(index, new KeyValuePair<GameObject, int>(dropEntry.Key, Mathf.RoundToInt(newAmount)));
                }
            }
        }
    }
}
