using Jotunn.Managers;
using System;
using System.Reflection;
using UnityEngine;

namespace Common {

    // The per-assembly facade onto the shared StartupPopupQueue, built the same way as ConfigUILauncher:
    // the first copy to run creates the queue's DontDestroyOnLoad GameObject, and every copy talks to
    // whichever queue is on it through reflection.
    //
    // Nothing in here may ever throw at a caller: a mod's Awake must not die because another mod shipped
    // an odd version of this folder.
    internal static class ConfigUIStartupPopups {
        // Order conventions, shared by every mod: lower shows first, and equal orders show in the order
        // they were queued. Stay inside the band that fits, offset within it if a mod has several.
        internal const int OrderWelcome = 100;   // first-run tutorials and welcomes
        internal const int OrderNotice = 200;    // anything else: update notices, migration prompts

        private static Component cachedQueue;
        private static MethodInfo cachedEnqueue;
        private static MethodInfo cachedCancel;
        private static bool loggedOwner;

        // Queues a popup to open on the main menu once it has settled and nothing else is showing. See
        // StartupPopupQueue.Enqueue for the contract tryOpen and isOpen must keep. Safe to call from Awake:
        // the queue waits for the main menu itself. Does nothing on a dedicated server.
        internal static bool Enqueue(string key, int order, Func<bool> tryOpen, Func<bool> isOpen) {
            if (string.IsNullOrEmpty(key) || tryOpen == null || isOpen == null) { return false; }
            if (GUIManager.IsHeadless()) { return false; }

            Component queue = Resolve();
            if (queue == null) { return false; }
            if (cachedEnqueue == null) {
                ModLogger.LogError("The startup popup queue on this machine has no compatible Enqueue method; " +
                    $"'{key}' will not be shown.");
                return false;
            }

            try {
                return (bool)cachedEnqueue.Invoke(queue, new object[] { key, order, tryOpen, isOpen });
            } catch (Exception e) {
                ModLogger.LogError($"Could not queue the startup popup '{key}': {e.Message}");
                return false;
            }
        }

        internal static void Cancel(string key) {
            if (string.IsNullOrEmpty(key) || GUIManager.IsHeadless()) { return; }
            Component queue = Resolve();
            if (queue == null || cachedCancel == null) { return; }
            try {
                cachedCancel.Invoke(queue, new object[] { key });
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not cancel the startup popup '{key}': {e.Message}");
            }
        }

        private static Component Resolve() {
            // Unity fake-null: a real comparison against null, not a ReferenceEquals.
            if (cachedQueue != null) { return cachedQueue; }
            cachedEnqueue = null;
            cachedCancel = null;

            GameObject host = null;
            try {
                host = GameObject.Find(StartupPopupQueue.QueueObjectName);
            } catch (Exception) {
                // Find can throw very early in load; treat it as "not there yet".
            }

            if (host == null) {
                // First copy to get here owns the queue. Never hidden and never inactive: GameObject.Find
                // skips inactive objects, and a queue nobody can find is one that every later mod duplicates.
                try {
                    host = new GameObject(StartupPopupQueue.QueueObjectName);
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    host.AddComponent<StartupPopupQueue>();
                } catch (Exception e) {
                    ModLogger.LogError($"Could not create the startup popup queue: {e.Message}");
                    return null;
                }
            }

            Component found = null;
            foreach (Component component in host.GetComponents<Component>()) {
                // Type NAME, not the type itself: the queue may well be another assembly's copy.
                if (component != null && component.GetType().Name == StartupPopupQueue.QueueTypeName) {
                    found = component;
                    break;
                }
            }
            if (found == null) {
                // Nothing but this contract ever names an object this, so one without a queue on it is a
                // leftover, and nothing else hangs off it -- unlike the launcher's object, adding one is safe.
                try {
                    found = host.AddComponent<StartupPopupQueue>();
                } catch (Exception e) {
                    ModLogger.LogError($"Could not create the startup popup queue: {e.Message}");
                    return null;
                }
            }

            Type type = found.GetType();
            // Bound by exact signature, never by name alone, so an additive amendment cannot make GetMethod
            // ambiguous.
            cachedEnqueue = type.GetMethod("Enqueue", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(string), typeof(int), typeof(Func<bool>), typeof(Func<bool>) }, null);
            cachedCancel = type.GetMethod("Cancel", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(string) }, null);

            if (loggedOwner == false) {
                loggedOwner = true;
                int hostVersion = 0;
                try {
                    PropertyInfo version = type.GetProperty("QueueVersion", BindingFlags.Public | BindingFlags.Instance);
                    if (version != null) { hostVersion = (int)version.GetValue(found, null); }
                } catch (Exception) {
                    // Advisory only.
                }
                // One line, at Info, naming the owner: the line that answers "why did the popups not queue".
                ModLogger.LogInfo($"Startup popup queue v{hostVersion} owned by " +
                    $"{type.Assembly.GetName().Name}; this mod carries v{StartupPopupQueue.ContractVersion}.");
            }

            cachedQueue = found;
            return cachedQueue;
        }
    }
}
