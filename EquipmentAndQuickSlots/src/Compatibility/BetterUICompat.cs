using BepInEx.Bootstrap;
using Common;

namespace EquipmentAndQuickSlots {
    // BetterUI (MK_BetterUI) needs no hooks of its own here. This records what it does to this
    // mod's UI, so the code that has to survive it is written knowingly, and it puts a name to the
    // one behavior worth telling the player about.
    //
    //  - Quick slot bar: BetterUI's Hud.Awake postfix looks up hudroot/QuickSlotsHotkeyBar (the
    //    name QuickSlotsHotBar gives its clone of the vanilla hotbar), destroys the bar's
    //    ConfigPositionedElement, and from then on positions and scales the bar from its own saved
    //    HUD layout, edited in-game with its HUD edit key (F7 by default). Its starting point is
    //    wherever the bar is at that moment — which is why QuickSlotsHotBar positions the bar the
    //    instant it creates it instead of leaving that to the component's first Update.
    //  - Inventory panel: BetterUI re-applies anchors and a scale to the vanilla "Player" panel at
    //    Hud.Awake. The scale is built with the two-argument Vector3 constructor, so its z is 0;
    //    EquipmentPanel therefore never converts world positions back into the panel's local space.
    //  - Grid cells: BetterUI's InventoryGrid.UpdateGui postfix walks m_elements by grid position
    //    to recolor durability bars and draw quality stars. Slot cells are real grid elements, so
    //    that works on them unchanged. Inactive cells are parked under an inactive holder rather
    //    than merely deactivated, so nothing walking the grid can bring them back into view.
    internal static class BetterUICompat {
        public const string BetterUIGUID = "MK_BetterUI";

        public static bool IsLoaded => Chainloader.PluginInfos.ContainsKey(BetterUIGUID);

        private static bool _handoffNoted;

        // Called from the hotbar controller, which only runs once a player exists — long after
        // Hud.Awake, so BetterUI's deferred Destroy of the positioner has gone through by then.
        // Once it has, this mod's position settings no longer place the bar; say so once, so a
        // player wondering why "Quick Slots Position" does nothing finds the answer in the log.
        internal static void NoteQuickBarHandoff(HotkeyBar quickBar) {
            if (_handoffNoted || quickBar == null)
                return;

            _handoffNoted = true;

            if (IsLoaded && quickBar.GetComponent<ConfigPositionedElement>() == null)
                EquipmentAndQuickSlots.LogInfo("BetterUI's HUD editor is positioning the quick slot bar; 'Quick Slots Anchor' and 'Quick Slots Position' only set where it starts. Move the bar with BetterUI's HUD edit key (F7 by default).");
        }
    }
}
