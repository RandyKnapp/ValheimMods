using HarmonyLib;

namespace EpicLoot.QuickConfig;

// The setup wizard no longer needs a FejdStartup.Start hook: QuickConfigureTool.Init queues it on the
// shared startup popup queue (Common/src/Config/UI), which waits for the main menu itself.

// Valheim reads Escape inline in Menu.Update, so while the panel is open the press must be swallowed
// for that frame or it would also collapse the pause menu the panel was opened from. The broker's own
// Menu.Update prefix handles only its mod list, and the Menu.Show prefix in EnchantingUI_Patches only
// blocks the pause menu over the enchanting table, so neither conflicts.
[HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
internal static class QuickConfig_Menu_Update_Patch {
    private static bool Prefix() {
        return QuickConfigureTool.TakeEscape() == false;
    }
}
