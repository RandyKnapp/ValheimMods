using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
	public static class Riches_CharacterDrop_GenerateDropList_Patch
	{
        public static readonly Dictionary<GameObject, int> RichesTable = new Dictionary<GameObject, int>
        {
            { ObjectDB.instance.GetItemPrefab("SilverNecklace"), 30 },
            { ObjectDB.instance.GetItemPrefab("Ruby"), 20 },
            { ObjectDB.instance.GetItemPrefab("AmberPearl"), 10 },
            { ObjectDB.instance.GetItemPrefab("Amber"), 5 },
            { ObjectDB.instance.GetItemPrefab("Coins"), 1 },
        };

        [UsedImplicitly]
		private static void Postfix(CharacterDrop __instance, ref List<KeyValuePair<GameObject, int>> __result)
		{
			var playerList = new List<Player>();
			Player.GetPlayersInRange(__instance.m_character.transform.position, 100f, playerList);

			var richesAmount = Random.Range(10, 100);
			var richesChance = playerList.Sum(player => player.m_nview.GetZDO().GetInt("el-rch")) * 0.01f;
			if (richesChance > 1)
			{
				richesAmount = Mathf.RoundToInt(richesAmount * richesChance);
			}

			if (Random.Range(0f, 1f) < richesChance)
			{
				foreach (var richesKv in RichesTable)
				{
					if (richesKv.Value <= richesAmount)
					{
						var amount = richesAmount / richesKv.Value;
						__result.Add(new KeyValuePair<GameObject, int>(richesKv.Key, amount));
						richesAmount -= amount * richesKv.Value;
					}
				}
			}
		}
	}
}