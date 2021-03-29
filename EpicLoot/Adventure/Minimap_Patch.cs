using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(Minimap))]
    public static class Minimap_Patch
    {
        public class TreasureMapPins
        {
            public Minimap.PinData Pin;
            public Minimap.PinData Area;
        }

        public static Dictionary<Tuple<int, Heightmap.Biome>, TreasureMapPins> Pins = new Dictionary<Tuple<int, Heightmap.Biome>, TreasureMapPins>();

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix(Minimap __instance)
        {
            if (!__instance.m_icons.Exists(x => x.m_name == EpicLoot.TreasureMapPinType))
            {
                __instance.m_icons.Add(new Minimap.SpriteData { m_name = EpicLoot.TreasureMapPinType, m_icon = EpicLoot.Assets.MapIconTreasureMap });
            }
            if (!__instance.m_icons.Exists(x => x.m_name == EpicLoot.BountyPinType))
            {
                __instance.m_icons.Add(new Minimap.SpriteData { m_name = EpicLoot.BountyPinType, m_icon = EpicLoot.Assets.MapIconBounty });
            }
        }

        [HarmonyPatch("UpdateDynamicPins")]
        [HarmonyPostfix]
        public static void UpdateDynamicPins_Postfix(Minimap __instance)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            var adventureSaveData = player.GetAdventureSaveData();
            if (adventureSaveData == null)
            {
                return;
            }

            var unfoundTreasureChests = adventureSaveData.GetUnfoundTreasureChests();
            var oldPins = Pins.Where(pinEntry => !unfoundTreasureChests.Exists(x => x.Interval == pinEntry.Key.Item1 && x.Biome == pinEntry.Key.Item2)).ToList();
            foreach (var pinEntry in oldPins)
            {
                __instance.RemovePin(pinEntry.Value.Pin);
                __instance.RemovePin(pinEntry.Value.Area);
                Pins.Remove(pinEntry.Key);
            }

            foreach (var chestInfo in unfoundTreasureChests)
            {
                var key = new Tuple<int, Heightmap.Biome>(chestInfo.Interval, chestInfo.Biome);
                if (!Pins.ContainsKey(key))
                {
                    var position = chestInfo.Position + chestInfo.MinimapCircleOffset;
                    var area = __instance.AddPin(position, Minimap.PinType.EventArea, string.Empty, false, false);
                    area.m_worldSize = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * 2;
                    var pin = __instance.AddPin(position, EpicLoot.TreasureMapPinType, $"Treasure Chest: {chestInfo.Biome}", false, false);

                    Pins.Add(key, new TreasureMapPins(){ Pin = pin, Area = area });
                }
            }
        }
    }
}
