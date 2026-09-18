using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Vanilla's trade item is a [Serializable] class, so entries authored on the Haldor/Hildir prefab
    /// get "" for their string fields from Unity's serializer. A `new Trader.TradeItem { ... }` built
    /// in code leaves them null, and vanilla StoreGui.FillList reads `tradeItem.m_tooltip.Length` with
    /// no guard -- one NullReferenceException, thrown after Show has already activated the window and
    /// before the list is finished, and nothing in the log names the mod that supplied the item.
    /// BuySelectedItem has the same hole in `m_buyPlayerEffects.Create(...)`.
    ///
    /// Filling those in here costs a walk over a handful of items and makes the store immune to the
    /// whole class, whichever mod stocks it. It also names the item once, since the first pass is the
    /// only one that finds it null.
    /// </summary>
    [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems))]
    public static class Trader_GetAvailableItems_NormalizeStock
    {
        [HarmonyPostfix]
        public static void Postfix(Trader __instance, List<Trader.TradeItem> __result)
        {
            if (__result == null)
            {
                return;
            }

            for (var i = 0; i < __result.Count; i++)
            {
                var item = __result[i];
                if (item == null)
                {
                    continue;
                }

                if (item.m_tooltip == null || item.m_name == null || item.m_buyPlayerEffects == null)
                {
                    var itemName = item.m_prefab != null ? item.m_prefab.name : item.m_name ?? "(unnamed)";
                    EpicLoot.LogWarningForce(
                        $"[Trader] '{(__instance != null ? __instance.m_name : "?")}' is stocking '{itemName}' with " +
                        "null fields (m_tooltip/m_name/m_buyPlayerEffects). Whichever mod added it built the " +
                        "TradeItem in code without the values Unity's serializer would have given it, which " +
                        "crashes vanilla StoreGui.FillList. Filling them in so the store still opens.");

                    item.m_tooltip = item.m_tooltip ?? "";
                    item.m_name = item.m_name ?? "";
                    item.m_buyPlayerEffects = item.m_buyPlayerEffects ?? new EffectList();
                }
            }
        }
    }

    /// <summary>
    /// StoreGui.FillList clamps its remembered index up to 0 and then hands it to SelectItem, which
    /// does `m_trader.GetAvailableItems()[index]` -- so a trader whose entire stock is gated behind
    /// keys the player does not have throws ArgumentOutOfRangeException the moment the store opens.
    /// SelectItem also re-queries GetAvailableItems a third time per fill, so a mod postfixing that
    /// with anything non-deterministic can hand it an index the new list does not have.
    ///
    /// Clamping the index leaves the normal path untouched: -1 is what vanilla itself uses for
    /// "nothing selected", and its own code already handles it.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SelectItem))]
    public static class StoreGui_SelectItem_ClampIndex
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(StoreGui), nameof(StoreGui.SelectItem)) != null;
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(StoreGui __instance, ref int __0)
        {
            if (__instance == null || __instance.m_trader == null || __0 < 0)
            {
                return;
            }

            var rows = __instance.m_itemList == null ? 0 : __instance.m_itemList.Count;
            var available = __instance.m_trader.GetAvailableItems();
            var max = Math.Min(rows, available == null ? 0 : available.Count);
            if (__0 >= max)
            {
                __0 = max - 1;
            }
        }
    }

    /// <summary>
    /// StoreGui.Show does nothing at all when the same trader is already considered visible, and
    /// "visible" is StoreGui.m_hiddenFrames, a counter only StoreGui.Update advances. If Update stops
    /// running with that counter low -- the component disabled, the window parented under something
    /// that went inactive, a second StoreGui holding the static instance -- then Show is a permanent
    /// silent no-op and pressing Use on the trader does nothing at all until the player relogs.
    ///
    /// The counter claiming "visible" while the window is not actually in an active hierarchy is that
    /// desync and nothing else, so push the counter past the threshold and let Show run its body. If
    /// the window still does not appear, the diagnostic line that follows says so.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Show))]
    public static class StoreGui_Show_RecoverFromVisibleDesync
    {
        [HarmonyPrefix]
        public static void Prefix(StoreGui __instance, Trader __0)
        {
            if (__instance == null || __0 == null)
            {
                return;
            }

            // Show will run its body on its own; nothing to repair.
            if (__instance.m_trader != __0 || !StoreGui.IsVisible())
            {
                return;
            }

            var rootPanel = __instance.m_rootPanel;
            if (rootPanel != null && rootPanel.activeInHierarchy)
            {
                return;
            }

            EpicLoot.LogWarningForce(
                $"[Trader] StoreGui says it is showing '{__0.m_name}' (hiddenFrames={__instance.m_hiddenFrames}) " +
                $"while its window is not in an active hierarchy (root={(rootPanel == null ? "null" : rootPanel.activeSelf ? "active" : "inactive")}, " +
                $"gui={__instance.gameObject.activeInHierarchy}). Show would have done nothing; forcing it to reopen.");

            __instance.m_hiddenFrames = 2;
            TraderDiagnostics.NoteRepaired();
        }
    }

    /// <summary>
    /// Vanilla StoreGui.Update closes the window on `ZInput.GetButtonDown("Use")` -- the very press
    /// that just opened it. Nothing consumes that press: GetButtonDown is a frame-latched flag that
    /// any number of callers read, and Player.Interact leaves it set. So whether the store survives
    /// its own opening frame comes down to which of Player.Update and StoreGui.Update Unity happens
    /// to run first, and neither script declares an execution order:
    ///
    ///   StoreGui.Update first -- the panel is still inactive, Update returns at the top. Fine, and
    ///                            this is the order the game normally ends up in.
    ///   Player.Update first   -- Show activates the panel, then Update sees it active with the press
    ///                            still latched and hides it again in the same frame.
    ///
    /// The second order draws nothing at all (activated and deactivated between two renders) and
    /// Hide() logs nothing, so it reaches us as "Use on the trader does nothing, and there is no
    /// error in the log". Relogging rebuilds both objects and can put the order back; a zone reload
    /// cannot, which is exactly the shape of the reports.
    ///
    /// Vanilla already knows about this hazard: InventoryGui guards the identical branch with
    /// `(m_shownFrames > 1) &amp; flag2`, refusing to close on the frame it opened. StoreGui has no
    /// m_shownFrames at all. This gives it that guard the way InventoryGui itself does it, with
    /// ZInput.ResetButtonStatus. Player.Interact has already consumed the press to open the store by
    /// the time this runs, so nothing later in the frame is left owing it.
    ///
    /// On a client that wins the ordering race this is inert: Show has not run yet when the prefix
    /// does, so the opening frame is never the current one.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui))]
    public static class StoreGui_KeepOpenOnItsOpeningFrame
    {
        /// <summary>Frame the window last went from closed to open. Never Time.frameCount otherwise.</summary>
        private static int _openedFrame = int.MinValue;

        /// <summary>Was the window already open when the Show now running started?</summary>
        private static bool _wasOpenBeforeShow;

        /// <summary>One line per session: the Update order does not change once it has been seen.</summary>
        private static bool _reported;

        /// <summary>
        /// Update is private, which is the kind of member a game update quietly renames. Gating the
        /// whole class on it keeps a miss from throwing out of CreateAndPatchAll, and leaves the Show
        /// bookkeeping off too -- it is of no use on its own.
        /// </summary>
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(StoreGui), nameof(StoreGui.Update)) != null;
        }

        [HarmonyPatch(nameof(StoreGui.Show))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Show_Prefix(StoreGui __instance)
        {
            _wasOpenBeforeShow = IsOpen(__instance);
        }

        /// <summary>
        /// A finalizer rather than a postfix, for the same reason the adventure panel uses one:
        /// FillList can throw after Show has already activated the window, and a half-filled window
        /// still on screen is the one worth protecting -- letting the same press close it again is
        /// how that throw went unreported in the first place.
        ///
        /// Only the closed-to-open transition counts. Show also runs its body for a press aimed at a
        /// different trader while the window is up, and pressing Use again on the same trader is the
        /// player asking to close it; recording either of those would swallow a close the player
        /// meant.
        /// </summary>
        [HarmonyPatch(nameof(StoreGui.Show))]
        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        public static void Show_Finalizer(StoreGui __instance)
        {
            if (!_wasOpenBeforeShow && IsOpen(__instance))
            {
                _openedFrame = Time.frameCount;
            }
        }

        [HarmonyPatch(nameof(StoreGui.Update))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Update_Prefix(StoreGui __instance)
        {
            // Vanilla's own first act is to return here, and every Hide it can reach is below that
            // line, so a closed store costs one bool per frame and nothing else.
            if (!IsOpen(__instance))
            {
                return;
            }

            var use = ZInput.GetButtonDown("Use");

            // Read here, before vanilla's ResetButtonStatus("JoyButtonB") on the line above its own
            // Hide() call makes the gamepad close unreadable from the Hide prefix.
            TraderDiagnostics.NoteUpdateCloseInputs(use, ZInput.GetKeyDown(KeyCode.Escape),
                ZInput.GetButtonDown("JoyButtonB"));

            if (!use || Time.frameCount != _openedFrame)
            {
                return;
            }

            ZInput.ResetButtonStatus("Use");

            if (_reported)
            {
                return;
            }

            _reported = true;
            EpicLoot.LogWarningForce(
                "[Trader] This client runs Player.Update before StoreGui.Update, so the Use press that " +
                "opens the store is still latched when StoreGui.Update reads it, and vanilla would have " +
                "closed the window in the same frame it opened -- with nothing drawn and nothing logged. " +
                "Swallowing that one press so the window stays open, which is the guard InventoryGui " +
                "already has and StoreGui does not. If the trader looked like it was ignoring you, this " +
                "was why.");
        }

        private static bool IsOpen(StoreGui storeGui)
        {
            return storeGui != null && storeGui.m_rootPanel != null && storeGui.m_rootPanel.activeSelf;
        }
    }
}
