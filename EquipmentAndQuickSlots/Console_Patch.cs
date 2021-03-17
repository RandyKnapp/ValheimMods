using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        private static readonly System.Random _random = new System.Random();

        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0 || !__instance.IsCheatsEnabled())
            {
                return true;
            }

            var command = args[0];
            if (command.Equals("resetinventory", StringComparison.InvariantCultureIgnoreCase))
            {
                Player.m_localPlayer.GetAllInventories().ForEach(x =>x.RemoveAll());
                return false;
            }

            return true;
        }
    }
}
