using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace EpicLoot.Adventure
{
    [HarmonyPatch(typeof(Minimap))]
    public static class Minimap_Patch
    {
        public const float AreaScale = 2.1f;

        public class AreaPinInfo
        {
            public Minimap.PinData Pin;
            public Minimap.PinData Area;
            public Minimap.PinData DebugPin;
        }

        public static Dictionary<Tuple<int, Heightmap.Biome>, AreaPinInfo> TreasureMapPins = new Dictionary<Tuple<int, Heightmap.Biome>, AreaPinInfo>();
        public static Dictionary<string, AreaPinInfo> BountyPins = new Dictionary<string, AreaPinInfo>();
        public static bool DebugMode;

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

        [HarmonyPatch(nameof(Minimap.Start))]
        [HarmonyPostfix]
        public static void Start_Postfix(Minimap __instance)
        {
            if (__instance.m_visibleIconTypes.Length < (int)EpicLoot.TreasureMapPinType + 1)
            {
                __instance.m_visibleIconTypes = new bool[(int)EpicLoot.TreasureMapPinType + 1];
                for (var index = 0; index < __instance.m_visibleIconTypes.Length; ++index)
                {
                    __instance.m_visibleIconTypes[index] = true;
                }
            }
        }

        [HarmonyPatch("OnDestroy")]
        [HarmonyPostfix]
        public static void OnDestroy_Postfix(Minimap __instance)
        {
            TreasureMapPins.Clear();
            BountyPins.Clear();
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
            var oldPins = TreasureMapPins.Where(pinEntry => !unfoundTreasureChests.Exists(x => x.Interval == pinEntry.Key.Item1 && x.Biome == pinEntry.Key.Item2)).ToList();
            foreach (var pinEntry in oldPins)
            {
                __instance.RemovePin(pinEntry.Value.Pin);
                __instance.RemovePin(pinEntry.Value.Area);
                if (pinEntry.Value.DebugPin != null)
                {
                    __instance.RemovePin(pinEntry.Value.DebugPin);
                }
                TreasureMapPins.Remove(pinEntry.Key);
            }

            foreach (var chestInfo in unfoundTreasureChests)
            {
                var key = new Tuple<int, Heightmap.Biome>(chestInfo.Interval, chestInfo.Biome);
                if (!TreasureMapPins.ContainsKey(key))
                {
                    var position = chestInfo.Position + chestInfo.MinimapCircleOffset;
                    var area = __instance.AddPin(position, Minimap.PinType.EventArea, string.Empty, false, false);
                    area.m_worldSize = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * AreaScale;
                    var label = Localization.instance.Localize("$mod_epicloot_treasurechest_minimappin", Localization.instance.Localize($"$biome_{chestInfo.Biome.ToString().ToLowerInvariant()}"), (chestInfo.Interval + 1).ToString());
                    var pin = __instance.AddPin(position, EpicLoot.TreasureMapPinType, label, false, false);

                    if (DebugMode)
                    {
                        var debugPin = __instance.AddPin(chestInfo.Position, Minimap.PinType.Icon3, $"{chestInfo.Position.x:0.0}, {chestInfo.Position.z:0.0}", false, false);
                        TreasureMapPins.Add(key, new AreaPinInfo() { Pin = pin, Area = area, DebugPin = debugPin });
                    }
                    else
                    {
                        TreasureMapPins.Add(key, new AreaPinInfo(){ Pin = pin, Area = area });
                    }
                }
            }

            var currentBounties = adventureSaveData.GetInProgressBounties();
            var oldBountyPins = BountyPins.Where(pinEntry => !currentBounties.Exists(x => x.ID == pinEntry.Key)).ToList();
            foreach (var pinEntry in oldBountyPins)
            {
                __instance.RemovePin(pinEntry.Value.Pin);
                __instance.RemovePin(pinEntry.Value.Area);
                BountyPins.Remove(pinEntry.Key);
            }

            foreach (var bounty in currentBounties)
            {
                var key = bounty.ID;
                if (!BountyPins.ContainsKey(key))
                {
                    var position = bounty.Position + bounty.MinimapCircleOffset;
                    var area = __instance.AddPin(position, Minimap.PinType.EventArea, string.Empty, false, false);
                    area.m_worldSize = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * AreaScale;
                    var label = Localization.instance.Localize("$mod_epicloot_bounties_minimappin", AdventureDataManager.GetBountyName(bounty));
                    var pin = __instance.AddPin(position, EpicLoot.BountyPinType, label, false, false);

                    if (DebugMode)
                    {
                        var debugPin = __instance.AddPin(bounty.Position, Minimap.PinType.Icon3, $"{bounty.Position.x:0.0}, {bounty.Position.z:0.0}", false, false);
                        BountyPins.Add(key, new AreaPinInfo() { Pin = pin, Area = area, DebugPin = debugPin });
                    }
                    else
                    {
                        BountyPins.Add(key, new AreaPinInfo() { Pin = pin, Area = area });
                    }
                }
            }
        }
    }
}
