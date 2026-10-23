using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common {

    // The shared queue for popups that open by themselves on the main menu -- first-run tutorials, update
    // notices -- so that however many mods have one, they show one at a time, in order, instead of all
    // landing on the same frame.
    //
    // ============================================================================================
    // FROZEN CROSS-ASSEMBLY CONTRACT -- v1. Never change the shape of anything marked below.
    //
    // Same arrangement as QuickConfigBroker, and deliberately NOT part of it: every mod that copies
    // Common/Config/UI/ compiles its own StartupPopupQueue, the first copy to run creates a
    // DontDestroyOnLoad GameObject named QueueObjectName, and every copy after that reaches the component
    // on it by TYPE NAME and method SIGNATURE, through reflection -- see ConfigUIStartupPopups.
    //
    // Its own GameObject, not the broker's, for two reasons. The broker belongs to whichever copy loaded
    // first, so a queue added to it as a v3 method would be missing whenever an older copy won that race.
    // And ConfigUILauncher only ever adds a broker to a launcher object it created itself: had a queue
    // created that object first, every older copy would find no broker on it and lose the Mod Config
    // button without a word.
    //
    // Only BCL types cross the boundary. Func<bool> lives in mscorlib, which every copy shares.
    //
    // Amendment rules: ADDITIVE ONLY, exactly as for the broker. A newer caller probes for a newer method
    // with GetMethod(...) != null and degrades silently when it is absent.
    // ============================================================================================
    internal class StartupPopupQueue : MonoBehaviour {
        // --- FROZEN ---
        internal const string QueueObjectName = "ModStartupPopupQueue";   // GameObject.Find key
        internal const string QueueTypeName = "StartupPopupQueue";        // Type.Name, NOT FullName
        // Every panel built with this kit carries a component of this type name, whichever copy built it:
        // ConfigUI.CreatePanel, the picker, the prompt and the broker's list all add one, and so does every
        // mod's own overlay. The queue reads it as "a panel is open, wait".
        internal const string PanelGuardTypeName = "ConfigUIInputGuard";
        internal const int ContractVersion = 1;

        public int QueueVersion {
            get { return ContractVersion; }
        }

        // Queues a popup. tryOpen is called once the main menu has settled and nothing else is showing: it
        // returns true when its popup is now up. From then on isOpen is polled every frame, and the next
        // popup waits until it returns false. isOpen must compare Unity objects with != null -- never ?., ??
        // or ReferenceEquals, which do not see a destroyed object as gone.
        //
        // Queuing a key that is already pending replaces its callbacks and order, keeping its place among
        // equal orders. Queuing the key that is showing right now is refused.
        public bool Enqueue(string key, int order, Func<bool> tryOpen, Func<bool> isOpen) {
            if (string.IsNullOrEmpty(key) || tryOpen == null || isOpen == null) { return false; }
            if (current != null && string.Equals(current.Key, key, StringComparison.Ordinal)) { return false; }
            Entry existing = FindPending(key);
            if (existing != null) {
                existing.Order = order;
                existing.TryOpen = tryOpen;
                existing.IsOpen = isOpen;
                return true;
            }
            pending.Add(new Entry { Key = key, Order = order, Sequence = nextSequence++, TryOpen = tryOpen, IsOpen = isOpen });
            return true;
        }

        // Drops a popup that has not shown yet. One that is already up stays up; closing it is its owner's job.
        public void Cancel(string key) {
            Entry existing = FindPending(key);
            if (existing != null) { pending.Remove(existing); }
        }

        // Pending or showing.
        public bool IsQueued(string key) {
            if (string.IsNullOrEmpty(key)) { return false; }
            if (current != null && string.Equals(current.Key, key, StringComparison.Ordinal)) { return true; }
            return FindPending(key) != null;
        }
        // --- END FROZEN ---

        // How long the menu must have been ready, without a break, before a popup opens: longer for the
        // first of a visit, so the menu is seen landing before it is covered.
        private const float FirstPopupDelay = 1f;
        private const float BetweenPopupsDelay = 0.5f;

        private sealed class Entry {
            internal string Key;
            internal int Order;
            internal long Sequence;
            internal Func<bool> TryOpen;
            internal Func<bool> IsOpen;
        }

        private readonly List<Entry> pending = new List<Entry>();
        private readonly List<MonoBehaviour> componentBuffer = new List<MonoBehaviour>();
        private Entry current;
        private long nextSequence;
        private float readySince = -1f;
        private bool shownThisVisit;
        private FejdStartup visit;

        public void Update() {
            if (current == null && pending.Count == 0) { return; }

            // Read fresh every frame and never cached past this method: FejdStartup.instance is nulled when
            // the start scene goes, and a held reference would be a destroyed object.
            FejdStartup startup = FejdStartup.instance;
            if (startup == null) {
                // Left the start scene. Whatever was showing went with it; what is still pending waits for
                // the next visit to the main menu.
                current = null;
                readySince = -1f;
                return;
            }
            if (startup != visit) {
                visit = startup;
                shownThisVisit = false;
                readySince = -1f;
            }

            if (current != null) {
                if (StillOpen(current)) { return; }
                current = null;
                readySince = -1f;
            }
            if (pending.Count == 0) { return; }

            // The gap is not only cosmetic. A closing panel is destroyed at the end of its frame, and that
            // is when its MainMenuGuard shows the main menu again -- under a popup opened in the same frame,
            // whose own guard had found the menu already hidden and so will never hide it. Enter in one of
            // that popup's text boxes would then start the game. Waiting for the menu to be seen active
            // again avoids that.
            if (MainMenuReady(startup) == false) {
                readySince = -1f;
                return;
            }
            float now = Time.unscaledTime;
            if (readySince < 0f) {
                readySince = now;
                return;
            }
            if (now - readySince < (shownThisVisit ? BetweenPopupsDelay : FirstPopupDelay)) { return; }

            // A popup that declines (its reason to show has gone since it was queued) is dropped, and the
            // next one gets its turn on the same frame.
            while (pending.Count > 0) {
                Entry next = TakeFirst();
                if (TryOpen(next)) {
                    current = next;
                    shownThisVisit = true;
                    readySince = -1f;
                    return;
                }
            }
        }

        // PlayIntroCinematic keeps m_mainMenu hidden until the video stops, whether it ends, is skipped, or
        // never plays. The menu list is inactive under the character and world pickers, which are not a
        // moment to interrupt either. The connection-failed notice is not a UnifiedPopup, so it is checked
        // on its own.
        private bool MainMenuReady(FejdStartup startup) {
            return CinematicsManager.IsStartedPlaying() == false
                && startup.m_mainMenu != null && startup.m_mainMenu.activeInHierarchy
                && startup.m_menuList != null && startup.m_menuList.activeInHierarchy
                && (startup.m_connectionFailedPanel == null || startup.m_connectionFailedPanel.activeInHierarchy == false)
                && UnifiedPopup.IsVisible() == false
                && GUIManager.CustomGUIFront != null
                && KitPanelOpen() == false;
        }

        // Not every panel hides the main menu while it is up -- a plain ConfigUI.CreatePanel does not -- so
        // the menu being active is not enough to say nothing is in the way. Every kit panel is a direct
        // child of CustomGUIFront, so only those are looked at.
        private bool KitPanelOpen() {
            Transform front = GUIManager.CustomGUIFront.transform;
            for (int i = 0; i < front.childCount; i++) {
                Transform child = front.GetChild(i);
                if (child.gameObject.activeSelf == false) { continue; }
                child.GetComponents(componentBuffer);
                foreach (MonoBehaviour component in componentBuffer) {
                    if (component != null && component.GetType().Name == PanelGuardTypeName) { return true; }
                }
            }
            return false;
        }

        private Entry FindPending(string key) {
            if (string.IsNullOrEmpty(key)) { return null; }
            foreach (Entry entry in pending) {
                if (string.Equals(entry.Key, key, StringComparison.Ordinal)) { return entry; }
            }
            return null;
        }

        // Lowest order first; equal orders in the order they were queued.
        private Entry TakeFirst() {
            Entry first = pending[0];
            foreach (Entry entry in pending) {
                if (entry.Order < first.Order || (entry.Order == first.Order && entry.Sequence < first.Sequence)) {
                    first = entry;
                }
            }
            pending.Remove(first);
            return first;
        }

        // Every callback is someone else's code reached through reflection. One mod's broken popup must not
        // stall the queue, or take any other mod's popup down with it.
        private static bool TryOpen(Entry entry) {
            bool opened = false;
            try {
                opened = entry.TryOpen();
            } catch (Exception e) {
                ModLogger.LogError($"The startup popup '{entry.Key}' failed to open: {e}");
            }
            if (opened) { return true; }
            // One that threw halfway may still have put a panel up. If so it is the current popup, and the
            // queue waits for it like any other.
            return StillOpen(entry);
        }

        private static bool StillOpen(Entry entry) {
            try {
                return entry.IsOpen();
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not tell whether the startup popup '{entry.Key}' is still open, treating it as closed: {e.Message}");
                return false;
            }
        }
    }
}
