using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EpicLoot.Crafting
{
    //public int GetLevel() => 1 + this.GetExtensions().Count;
    [HarmonyPatch(typeof(CraftingStation), "GetLevel")]
    public static class CraftingStation_GetLevel_Patch
    {
        public static bool Prefix(CraftingStation __instance, ref int __result)
        {
            __result = 1 + __instance.GetExtensions().Count(x => x.m_piece.m_isUpgrade);
            return false;
        }
    }
}
