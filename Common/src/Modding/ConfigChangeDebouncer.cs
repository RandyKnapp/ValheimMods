using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common {
    // Debounces config apply work so a burst of rapid SettingChanged events for one entry
    // (a user typing into a field, a config file reload, or a server config sync) collapses
    // into a single apply once the edits settle, instead of re-doing the heavy work (recipe
    // rebuilds, world scans) on every intermediate value.
    //
    // Mirrors the EnqueueWorldUpdate/DrainWorldUpdates coroutine pattern (BepInEx ThreadingHelper,
    // yielding null each frame - WaitForSeconds is not reliably honoured by that runner). No locking;
    // like EnqueueWorldUpdate this assumes the config handlers run on the main thread.
    public static class ConfigChangeDebouncer {
        // Latest action to run per key (the changed ConfigEntry instance).
        private static readonly Dictionary<object, Action> pendingActions = new Dictionary<object, Action>();
        // Time (Time.realtimeSinceStartup) at which each key's action should fire.
        private static readonly Dictionary<object, float> fireAt = new Dictionary<object, float>();
        // Keys with a coroutine already waiting, so we don't start a second one.
        private static readonly HashSet<object> running = new HashSet<object>();

        // Schedules action to run after ModContext.ConfigApplyDelay seconds. Re-calling with the same
        // key before it fires replaces the action and resets the timer (true debounce + coalesce).
        // A delay <= 0 applies immediately (lets admins disable the delay).
        public static void Schedule(object key, Action action) {
            float delay = ModContext.ConfigApplyDelay != null ? ModContext.ConfigApplyDelay.Value : 0f;
            if (delay <= 0f) {
                action();
                return;
            }
            // During game shutdown the ThreadingHelper's MonoBehaviour is destroyed while config entries can
            // still fire SettingChanged (e.g. Jotunn reverting server-synced values on disconnect). Calling
            // StartCoroutine on a destroyed behaviour throws ArgumentNullException, and there's nothing left
            // to update anyway, so drop the change. The Unity '==' overload treats a destroyed object as null.
            BepInEx.ThreadingHelper host = BepInEx.ThreadingHelper.Instance;
            if (host == null) { return; }
            pendingActions[key] = action;
            fireAt[key] = Time.realtimeSinceStartup + delay;
            if (running.Contains(key)) { return; }
            running.Add(key);
            host.StartCoroutine(Run(key));
        }

        private static IEnumerator Run(object key) {
            // Wait until the entry has been idle for the full delay; re-scheduling pushes fireAt out.
            while (fireAt.TryGetValue(key, out float at) && Time.realtimeSinceStartup < at) {
                yield return null;
            }
            pendingActions.TryGetValue(key, out Action action);
            pendingActions.Remove(key);
            fireAt.Remove(key);
            running.Remove(key);
            action?.Invoke();
        }
    }
}
