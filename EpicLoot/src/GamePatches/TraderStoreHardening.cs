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
    /// desync and nothing else, so push the counter past the threshold and let Show run its body.
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
        }
    }

    /// <summary>
    /// Vanilla StoreGui.Update closes the window on `ZInput.GetButtonDown("Use")`, a frame-latched flag
    /// that the press which just opened it leaves set. When Unity runs Player.Update before
    /// StoreGui.Update (neither declares an execution order), the store opens and closes in the same
    /// frame with nothing drawn or logged. InventoryGui guards the same branch with m_shownFrames;
    /// StoreGui has none, so this swallows the Use press on the opening frame with ResetButtonStatus.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui))]
    public static class StoreGui_KeepOpenOnItsOpeningFrame
    {
        private static int _openedFrame = int.MinValue;
        private static bool _wasOpenBeforeShow;
        private static bool _reported;

        // Update is private, the kind of member a game update renames; gating the whole class on it
        // keeps a miss from failing the patch and leaves the Show bookkeeping off with it.
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

        // A finalizer because FillList can throw after Show has activated the window. Only the
        // closed-to-open transition counts: Show also runs for a press on another trader while open,
        // and Use again on the same trader is a close the player meant.
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
            if (!IsOpen(__instance))
            {
                return;
            }

            var use = ZInput.GetButtonDown("Use");
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
