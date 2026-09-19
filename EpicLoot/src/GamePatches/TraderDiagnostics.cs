using System;
using System.Collections.Generic;
using System.Diagnostics;
using EpicLoot.Adventure;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Reports what happened between a Use press aimed at a trader and the store window appearing.
    ///
    /// Every way that path can fail is silent by construction, which is why "the trader stopped
    /// responding" arrives with nothing in the log:
    ///
    ///   - <see cref="StoreGui.Show"/> returns without doing anything when the same trader is already
    ///     considered visible, and "visible" is a frame counter (m_hiddenFrames) that only advances
    ///     from StoreGui.Update -- so if Update stalls, Show is a permanent no-op.
    ///   - Show activates m_rootPanel, but SetActive on an object under an inactive ancestor, or on a
    ///     second StoreGui that grabbed the static instance, renders nothing and raises nothing.
    ///   - StoreGui.Update hides the window again on the Use press that opened it, on
    ///     InventoryGui.IsVisible(), on distance, and on a null trader. Hide() logs nothing.
    ///   - A prefix elsewhere returning false, or a blocked hover, means Trader.Interact never runs.
    ///
    /// This is deliberately **not** behind a config entry and deliberately uses the *Force log
    /// variants: the only logs we ever get for this are the ones players attach to a report, and they
    /// have not switched anything on. Cost is one line per non-held Use press on a trader, a null
    /// check on every other Use press, and nothing at all per frame. Anything beyond that one line
    /// means something actually went wrong.
    ///
    /// Reading the summary line: `trader=` did Trader.Interact run, `show=` did Show run its body,
    /// `fill=` how many rows the vanilla list ended up with, `root=`/`gui=` whether the window is
    /// active *and* in an active hierarchy, `inst=self` whether StoreGui.instance is the object we
    /// were called on.
    /// </summary>
    public static class TraderDiagnostics
    {
        private class PressRecord
        {
            public int Frame;
            public string TraderName = "?";
            public string HoverName = "?";
            public bool TakeInput;
            public bool ReachedTrader;
            public bool ShowCalled;
            public bool ShowBodyRan;
            public bool Repaired;
            public bool SameTrader;
            public bool WasVisible;
            public int HiddenFramesBefore;
            public bool InstanceIsSelf = true;
            public bool RootActiveSelf;
            public bool RootInHierarchy;
            public bool GuiInHierarchy;
            public int FillCount = -1;
            public string FillError;
        }

        private static PressRecord _press;

        /// <summary>Frame the store window was last actually opened, for the closed-too-soon check.</summary>
        private static int _lastOpenedFrame = int.MinValue;

        internal static bool Recording => _press != null;

        internal static void BeginPress(GameObject hovered, bool hold)
        {
            // Holding Use re-enters Player.Interact every frame; only a real press is an attempt to
            // open the store, and only a press is worth a line.
            if (hold || hovered == null)
            {
                return;
            }

            var trader = hovered.GetComponentInParent<Trader>();
            if (trader == null)
            {
                _press = null;
                return;
            }

            var player = Player.m_localPlayer;
            _press = new PressRecord
            {
                Frame = Time.frameCount,
                TraderName = trader.m_name,
                HoverName = hovered.name,
                TakeInput = player != null && player.TakeInput()
            };
        }

        internal static void MarkReachedTrader(Trader trader)
        {
            if (_press == null)
            {
                return;
            }

            _press.ReachedTrader = true;
            if (trader != null)
            {
                _press.TraderName = trader.m_name;
            }
        }

        internal static void MarkShowEntered(StoreGui storeGui, Trader trader)
        {
            if (_press == null || storeGui == null)
            {
                return;
            }

            _press.ShowCalled = true;
            _press.InstanceIsSelf = StoreGui.instance == storeGui;
            _press.SameTrader = storeGui.m_trader == trader;
            _press.WasVisible = StoreGui.IsVisible();
            _press.HiddenFramesBefore = storeGui.m_hiddenFrames;

            // Show's own condition, evaluated before it runs: this is what tells H1 apart from the rest.
            _press.ShowBodyRan = !_press.SameTrader || !_press.WasVisible;
        }

        /// <summary>
        /// Called by the show-desync repair in <see cref="StoreGui_Show_RecoverFromVisibleDesync"/>,
        /// which runs after this class's own Show prefix has already recorded the pre-repair decision.
        /// Without this the summary would report a skipped body for a press that did open the window.
        /// </summary>
        internal static void NoteRepaired()
        {
            if (_press == null)
            {
                return;
            }

            _press.Repaired = true;
            _press.ShowBodyRan = true;
        }

        internal static void MarkFilled(StoreGui storeGui, Exception exception)
        {
            if (exception != null)
            {
                // Worth a line even outside a tracked press: FillList also runs on buy and sell.
                EpicLoot.LogErrorForce($"[Trader] StoreGui.FillList threw, the store list is incomplete:\n{exception}");
            }

            if (_press == null)
            {
                return;
            }

            _press.FillCount = storeGui != null && storeGui.m_itemList != null ? storeGui.m_itemList.Count : -1;
            _press.FillError = exception?.GetType().Name;
        }

        internal static void MarkShowFinished(StoreGui storeGui)
        {
            if (_press == null || storeGui == null)
            {
                return;
            }

            var rootPanel = storeGui.m_rootPanel;
            _press.RootActiveSelf = rootPanel != null && rootPanel.activeSelf;
            _press.RootInHierarchy = rootPanel != null && rootPanel.activeInHierarchy;
            _press.GuiInHierarchy = storeGui.gameObject.activeInHierarchy;

            if (_press.ShowBodyRan && _press.RootActiveSelf)
            {
                _lastOpenedFrame = Time.frameCount;
            }
        }

        /// <summary>Emits the one line. Runs from a finalizer, so it happens however the press ended.</summary>
        internal static void EndPress(Exception exception)
        {
            var press = _press;
            _press = null;
            if (press == null)
            {
                return;
            }

            var problems = new List<string>();
            if (!press.TakeInput)
            {
                problems.Add("Player.TakeInput() was false (a GUI thinks it is open)");
            }

            if (!press.ReachedTrader)
            {
                problems.Add("Trader.Interact never ran (a prefix returned false, or the hover is not the trader)");
            }
            else if (!press.ShowCalled)
            {
                problems.Add("StoreGui.Show was never called");
            }
            else if (!press.ShowBodyRan)
            {
                problems.Add("Show skipped its body: this trader already counts as visible, so the window will not reopen until StoreGui.Update runs again");
            }
            else if (!press.RootInHierarchy)
            {
                problems.Add(press.RootActiveSelf
                    ? "the store window was activated but is under an inactive parent, so nothing is drawn"
                    : "the store window is not active after Show");
            }

            if (!press.InstanceIsSelf)
            {
                problems.Add("StoreGui.instance is a different StoreGui than the one that was shown");
            }

            if (press.FillError != null)
            {
                problems.Add("FillList threw " + press.FillError);
            }

            if (exception != null)
            {
                problems.Add("Player.Interact threw " + exception.GetType().Name);
            }

            var panel = StoreGui_Patch.MerchantPanel;
            var line =
                $"[Trader] Use on '{press.TraderName}' (hover={press.HoverName}): takeInput={press.TakeInput} " +
                $"trader={(press.ReachedTrader ? "ran" : "MISSED")} " +
                $"show={(press.ShowCalled ? press.Repaired ? "ran(repaired)" : press.ShowBodyRan ? "ran" : "skipped" : "MISSED")} " +
                $"sameTrader={press.SameTrader} wasVisible={press.WasVisible} hidden={press.HiddenFramesBefore} " +
                $"fill={press.FillCount} root={(press.RootActiveSelf ? "active" : "inactive")}" +
                $"{(press.RootInHierarchy ? "+shown" : "+hidden")} gui={(press.GuiInHierarchy ? "shown" : "HIDDEN")} " +
                $"inst={(press.InstanceIsSelf ? "self" : "OTHER")} " +
                $"panel={(panel == null ? "none" : panel.activeSelf ? "active" : "inactive")}";

            if (problems.Count == 0)
            {
                EpicLoot.LogForce(line);
                return;
            }

            line += "\n         problems: " + string.Join("; ", problems.ToArray());
            if (press.FillError != null || exception != null)
            {
                EpicLoot.LogErrorForce(line);
            }
            else
            {
                EpicLoot.LogWarningForce(line);
            }
        }

        /// <summary>
        /// The store closing within a frame or two of opening is the one failure the summary line
        /// cannot see: Show did everything right and something shut the window straight back down.
        /// Only that case pays for a stack trace; an ordinary close says nothing.
        /// </summary>
        internal static void NoteHide(StoreGui storeGui)
        {
            var sinceOpen = Time.frameCount - _lastOpenedFrame;
            if (storeGui == null || sinceOpen < 0 || sinceOpen > 2)
            {
                return;
            }

            _lastOpenedFrame = int.MinValue;

            var player = Player.m_localPlayer;
            var distance = player != null && storeGui.m_trader != null
                ? Vector3.Distance(storeGui.m_trader.transform.position, player.transform.position)
                : -1f;

            EpicLoot.LogWarningForce(
                $"[Trader] The store window closed {sinceOpen} frame(s) after it opened. " +
                $"traderNull={storeGui.m_trader == null} distance={distance:F1}/{storeGui.m_hideDistance} " +
                $"invGui={InventoryGui.IsVisible()} map={Minimap.IsOpen()} " +
                $"use={ZInput.GetButtonDown("Use")} esc={ZInput.GetKeyDown(KeyCode.Escape)} " +
                $"joyB={ZInput.GetButtonDown("JoyButtonB")} textViewer={(TextViewer.instance != null)}\n" +
                new StackTrace(1, false));
        }

        internal static void Failed(string where, Exception e)
        {
            EpicLoot.LogErrorForce($"[Trader] Trader diagnostics failed in {where} (this is the diagnostic's own bug, " +
                                   $"not the trader's):\n{e}");
        }
    }

    /// <summary>
    /// Opens and closes the record. Parameters are injected by index rather than by name so a
    /// renamed vanilla parameter cannot stop the whole mod from patching.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Interact))]
    public static class Player_Interact_TraderDiagnostics
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.Interact),
                new[] { typeof(GameObject), typeof(bool), typeof(bool) }) != null;
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(GameObject __0, bool __1)
        {
            try
            {
                TraderDiagnostics.BeginPress(__0, __1);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("Player.Interact prefix", e);
            }
        }

        [HarmonyPriority(Priority.Last)]
        public static void Finalizer(Exception __exception)
        {
            try
            {
                TraderDiagnostics.EndPress(__exception);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("Player.Interact finalizer", e);
            }
        }
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.Interact))]
    public static class Trader_Interact_TraderDiagnostics
    {
        [HarmonyPriority(Priority.First)]
        public static void Prefix(Trader __instance, bool __1)
        {
            try
            {
                if (!__1)
                {
                    TraderDiagnostics.MarkReachedTrader(__instance);
                }
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("Trader.Interact prefix", e);
            }
        }
    }

    [HarmonyPatch(typeof(StoreGui))]
    public static class StoreGui_TraderDiagnostics
    {
        [HarmonyPatch(nameof(StoreGui.Show))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Show_Prefix(StoreGui __instance, Trader __0)
        {
            try
            {
                TraderDiagnostics.MarkShowEntered(__instance, __0);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("StoreGui.Show prefix", e);
            }
        }

        // Last, so the state it reports includes whatever the adventure panel finalizer did.
        [HarmonyPatch(nameof(StoreGui.Show))]
        [HarmonyFinalizer]
        [HarmonyPriority(Priority.Last)]
        public static void Show_Finalizer(StoreGui __instance)
        {
            try
            {
                TraderDiagnostics.MarkShowFinished(__instance);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("StoreGui.Show finalizer", e);
            }
        }

        [HarmonyPatch(nameof(StoreGui.Hide))]
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Hide_Prefix(StoreGui __instance)
        {
            try
            {
                TraderDiagnostics.NoteHide(__instance);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("StoreGui.Hide prefix", e);
            }
        }
    }

    /// <summary>
    /// Its own class so Prepare can skip just this one: FillList is private, and a private method is
    /// the kind that quietly changes shape in a game update. A missing target would otherwise throw
    /// out of CreateAndPatchAll and take the whole mod with it.
    /// </summary>
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList))]
    public static class StoreGui_FillList_TraderDiagnostics
    {
        public static bool Prepare()
        {
            return AccessTools.Method(typeof(StoreGui), nameof(StoreGui.FillList)) != null;
        }

        [HarmonyFinalizer]
        public static void Finalizer(StoreGui __instance, Exception __exception)
        {
            try
            {
                TraderDiagnostics.MarkFilled(__instance, __exception);
            }
            catch (Exception e)
            {
                TraderDiagnostics.Failed("StoreGui.FillList finalizer", e);
            }
        }
    }
}
