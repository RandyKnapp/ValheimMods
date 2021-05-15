using System.Linq;
using HarmonyLib;

namespace EpicLoot.Crafting
{
    //public int GetLevel() => 1 + this.GetExtensions().Count;
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetLevel))]
    public static class CraftingStation_GetLevel_Patch
    {
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(CraftingStation __instance, ref int __result)
        {
            __result = 1 + __instance.GetExtensions().Count(x => x.m_piece.m_isUpgrade);
            return false;
        }
    }
}
