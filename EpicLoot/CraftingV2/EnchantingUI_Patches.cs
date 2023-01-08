using EpicLoot_UnityLib;
using HarmonyLib;

namespace EpicLoot.CraftingV2
{
    [HarmonyPatch]
    public static class EnchantingUI_Patches
    {
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.IsOpen))]
        [HarmonyPostfix]
        public static void Minimap_IsOpen_Postfix(ref bool __result)
        {
            if (EnchantingTableUI.IsVisible())
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
        [HarmonyPostfix]
        public static void Player_TakeInput_Postfix(ref bool __result)
        {
            if (EnchantingTableUI.IsVisible())
            {
                __result = false;
            }
        }

        [HarmonyPatch(typeof(Minimap), nameof(Minimap.InTextInput))]
        [HarmonyPostfix]
        public static void Minimap_InTextInput_Postfix(ref bool __result)
        {
            if (EnchantingTableUI.IsVisible() && EnchantingTableUI.IsInTextInput())
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.Show))]
        [HarmonyPrefix]
        public static bool Menu_Show_Prefix()
        {
            if (EnchantingTableUI.IsVisible())
                return false;

            return true;
        }
    }
}