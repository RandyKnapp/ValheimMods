using System;
using System.Reflection;
using UnityEngine;

namespace Common {

    // The per-assembly facade onto the shared QuickConfig launcher.
    //
    // Several mods may each carry their own copy of this folder, and each compiles its own
    // QuickConfigBroker into its own assembly. Those types are unrelated to one another as far as the CLR
    // is concerned, so a plain static registry would give every mod its own private launcher and every
    // mod its own button. Instead the FIRST copy to run creates a DontDestroyOnLoad GameObject with a
    // frozen name, and every copy afterwards finds it and talks to whatever broker is already on it
    // through reflection.
    //
    // Nothing in here may ever throw at a caller: a mod's Awake must not die because another mod shipped
    // an odd version of this folder.
    internal static class ConfigUILauncher {
        private static Component cachedBroker;
        private static MethodInfo cachedRegister;
        private static MethodInfo cachedUnregister;
        private static bool loggedOwner;

        internal static bool IsAvailable {
            get { return Resolve() != null; }
        }

        // Optional: creates the broker eagerly so the button exists even before the first Register. Most
        // mods can skip this and just call Register.
        internal static void Init() {
            Resolve();
        }

        internal static bool Register(string displayName, Action openPanel) {
            if (string.IsNullOrEmpty(displayName) || openPanel == null) { return false; }

            Component broker = Resolve();
            if (broker == null) { return false; }
            if (cachedRegister == null) {
                ModLogger.LogError("The QuickConfig launcher on this machine has no compatible Register method; " +
                    $"'{displayName}' will not appear in it.");
                return false;
            }

            try {
                cachedRegister.Invoke(broker, new object[] { displayName, openPanel });
                return true;
            } catch (Exception e) {
                ModLogger.LogError($"Could not register '{displayName}' with the QuickConfig launcher: {e.Message}");
                return false;
            }
        }

        internal static void Unregister(string displayName) {
            if (string.IsNullOrEmpty(displayName)) { return; }
            Component broker = Resolve();
            if (broker == null || cachedUnregister == null) { return; }
            try {
                cachedUnregister.Invoke(broker, new object[] { displayName });
            } catch (Exception e) {
                ModLogger.LogWarning($"Could not unregister '{displayName}' from the QuickConfig launcher: {e.Message}");
            }
        }

        private static Component Resolve() {
            // Unity fake-null: a cached component can be destroyed with its scene even though the C#
            // reference is not literally null, so this must be a real comparison against null and not a
            // ReferenceEquals.
            if (cachedBroker != null) { return cachedBroker; }
            cachedRegister = null;
            cachedUnregister = null;

            GameObject host = null;
            try {
                host = GameObject.Find(QuickConfigBroker.BrokerObjectName);
            } catch (Exception) {
                // Find can throw very early in load; treat it as "not there yet".
            }

            if (host == null) {
                // First copy to get here owns the launcher. Deliberately NOT hidden and never inactive:
                // GameObject.Find skips inactive objects, and a broker nobody can find is one that every
                // later mod duplicates.
                try {
                    host = new GameObject(QuickConfigBroker.BrokerObjectName);
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    host.AddComponent<QuickConfigBroker>();
                } catch (Exception e) {
                    ModLogger.LogError($"Could not create the QuickConfig launcher: {e.Message}");
                    return null;
                }
            }

            Component found = null;
            foreach (Component component in host.GetComponents<Component>()) {
                // Type NAME, not the type itself: the broker we are looking at may well be another
                // assembly's copy, which is a different Type entirely.
                if (component != null && component.GetType().Name == QuickConfigBroker.BrokerTypeName) {
                    found = component;
                    break;
                }
            }
            if (found == null) { return null; }

            Type type = found.GetType();
            // Bound by exact signature, never by name alone -- an additive amendment to the contract could
            // otherwise leave GetMethod ambiguous and throw.
            cachedRegister = type.GetMethod("Register", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(string), typeof(Action) }, null);
            cachedUnregister = type.GetMethod("Unregister", BindingFlags.Public | BindingFlags.Instance, null,
                new[] { typeof(string) }, null);

            if (loggedOwner == false) {
                loggedOwner = true;
                int hostVersion = 1;
                try {
                    PropertyInfo version = type.GetProperty("BrokerVersion", BindingFlags.Public | BindingFlags.Instance);
                    if (version != null) { hostVersion = (int)version.GetValue(found, null); }
                } catch (Exception) {
                    // Advisory only.
                }
                // One line, at Info, naming the owner. When a user reports "the config button is missing",
                // this is the line that says which mod's copy is in charge.
                ModLogger.LogInfo($"QuickConfig launcher v{hostVersion} owned by " +
                    $"{type.Assembly.GetName().Name}; this mod carries v{QuickConfigBroker.ContractVersion}.");
                if (hostVersion < QuickConfigBroker.ContractVersion) {
                    ModLogger.LogInfo("That copy is older than this one. Registration still works; the launcher " +
                        "UI is whatever the owning mod shipped.");
                }
            }

            cachedBroker = found;
            return cachedBroker;
        }
    }
}
