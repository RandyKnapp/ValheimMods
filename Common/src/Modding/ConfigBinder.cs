using BepInEx.Configuration;
using Jotunn.Extensions;

namespace Common {
    /// <summary>
    /// Binding helpers over <see cref="ModContext.Cfg"/>.
    ///
    /// BindServerConfig marks entries IsAdminOnly, which is what makes Jotunn's SynchronizationManager
    /// treat them as server-authoritative; BindClientConfig leaves them local to each player.
    ///
    /// Prefer the ...InOrder variants for new config files: they keep the settings grouped the way the
    /// code declares them instead of alphabetised. See the remarks on <see cref="BindServerConfigInOrder{T}"/>.
    /// </summary>
    public static class ConfigBinder {
        private static ConfigFile Cfg => ModContext.Cfg;

        // -- Server synced (admin only) ------------------------------------------------------------

        public static ConfigEntry<bool> BindServerConfig(string category, string key, bool value, string description,
                                                        AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<int> BindServerConfig(string category, string key, int value, string description,
                                                       bool advanced = false, int valMin = 0, int valMax = 150) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, new AcceptableValueRange<int>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<float> BindServerConfig(string category, string key, float value, string description,
                                                         bool advanced = false, float valMin = 0f, float valMax = 150f) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, new AcceptableValueRange<float>(valMin, valMax),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        public static ConfigEntry<string> BindServerConfig(string category, string key, string value, string description,
                                                          AcceptableValueList<string> acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        /// <summary>Generic escape hatch for types without a dedicated overload above.</summary>
        public static ConfigEntry<T> BindServerConfig<T>(string category, string key, T value, string description,
                                                        AcceptableValueBase acceptableValues, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, IsAdvanced = advanced }));
        }

        // -- Client local --------------------------------------------------------------------------

        public static ConfigEntry<T> BindClientConfig<T>(string category, string key, T value, string description,
                                                        AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.Bind(category, key, value,
                new ConfigDescription(description, acceptableValues,
                    new ConfigurationManagerAttributes { IsAdminOnly = false, IsAdvanced = advanced }));
        }

        // -- Ordered ---------------------------------------------------------------------------------

        /// <summary>
        /// Server-synced entry bound through Jotunn's ordered binder.
        /// </summary>
        /// <remarks>
        /// ConfigurationManager sorts sections alphabetically and, within a section, by the Order
        /// attribute. Jotunn's BindConfigInOrder exploits both: it prefixes the section with the position
        /// it was first bound in ("2 - Balance") and hands each entry a descending Order, so what the
        /// player sees matches the grouping the code declares rather than an alphabetical jumble.
        ///
        /// Two consequences to plan for:
        /// - Keep a config file to **nine sections or fewer**. The prefix is not zero padded, so a tenth
        ///   section sorts as "10 - ..." between "1 - ..." and "2 - ...".
        /// - The prefix is part of the section name written to disk, so inserting a section renames every
        ///   section after it and orphans the player's saved values. Add new sections at the end, or
        ///   migrate the old names (EpicLoot's ELConfig does the latter).
        /// </remarks>
        public static ConfigEntry<T> BindServerConfigInOrder<T>(string category, string key, T value, string description,
                                                               AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.BindConfigInOrder(category, key, value, description, synced: true,
                acceptableValues: acceptableValues,
                configAttributes: new ConfigurationManagerAttributes { IsAdvanced = advanced });
        }

        /// <summary>Client-local counterpart of <see cref="BindServerConfigInOrder{T}"/>.</summary>
        public static ConfigEntry<T> BindClientConfigInOrder<T>(string category, string key, T value, string description,
                                                               AcceptableValueBase acceptableValues = null, bool advanced = false) {
            return Cfg.BindConfigInOrder(category, key, value, description, synced: false,
                acceptableValues: acceptableValues,
                configAttributes: new ConfigurationManagerAttributes { IsAdvanced = advanced });
        }
    }
}
