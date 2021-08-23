using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{

    [HarmonyPatch(typeof(Minimap), "Explore", new Type[]
    {
        typeof(Vector3),
        typeof(float)
    })]
    public class MinimapDiscoveryRadius_Patch
    {

        public static void Prefix(ref Vector3 p, ref float radius)
        {
            if (PlayerIsOnControlledBoat())
            {
                float @float = EquipmentAndQuickSlots.MinimapDiscoveryRadiusBoat.Value;
                if (@float != 0)
                {
                    radius *= @float;
                }
            }
            else
            {
                float @float = EquipmentAndQuickSlots.MinimapDiscoveryRadius.Value;
                if (@float != 0)
                {
                    radius *= @float;
                }
            }
        }

        public static bool PlayerIsOnControlledBoat()
        {
            if (!Player.m_localPlayer)
            {
                return false;
            }
            if ((bool)Player.m_localPlayer.GetShipControl())
            {
                return true;
            }
            List<Player> list = new List<Player>(2);
            Player.GetPlayersInRange(Player.m_localPlayer.transform.position, 20f, list);
            foreach (Player item in list)
            {
                if ((bool)item.GetShipControl())
                {
                    return true;
                }
            }
            return false;
        }
    }
}