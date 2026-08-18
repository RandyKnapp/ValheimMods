using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Common {
    /// <summary>
    /// Loads every localization JSON embedded in the mod assembly, mirrors it to
    /// BepInEx/config/&lt;ConfigFolder&gt;/Localizations/&lt;Language&gt;.json so players can edit it, and
    /// registers the merged result with Jotunn.
    ///
    /// The on-disk copy wins for keys it already has; keys the mod newly ships are added, and keys the mod
    /// no longer ships are pruned. Files should be plain JSON objects named for a language from
    /// https://valheim-modding.github.io/Jotunn/data/localization/language-list.html
    /// </summary>
    public static class ModLocalization {
        /// <summary>Resource-name fragment identifying localization files, and the config subfolder name.</summary>
        public static string LocalizationFolder = "Localizations";

        public static void AddLocalizations(Assembly assembly = null) {
            if (assembly == null) { assembly = Assembly.GetExecutingAssembly(); }

            CustomLocalization localization = ModContext.Localization ?? LocalizationManager.Instance.GetLocalization();
            ModContext.Localization = localization;

            string translationFolder = Path.Combine(BepInEx.Paths.ConfigPath, ModContext.ConfigFolder ?? assembly.GetName().Name, LocalizationFolder);
            Directory.CreateDirectory(translationFolder);

            foreach (string embeddedResource in assembly.GetManifestResourceNames()) {
                if (embeddedResource.IndexOf(LocalizationFolder, StringComparison.OrdinalIgnoreCase) < 0) { continue; }
                if (!embeddedResource.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) { continue; }

                string language = LanguageFromResourceName(embeddedResource);
                if (string.IsNullOrEmpty(language)) {
                    ModLogger.LogWarning($"Could not derive a language name from '{embeddedResource}', skipping.");
                    continue;
                }

                // Comments are used in the shipped files but are not valid JSON, so strip them first.
                string shipped = Regex.Replace(ReadEmbeddedResourceFile(embeddedResource, assembly), @"\/\/.*", "");
                string onDiskPath = Path.Combine(translationFolder, $"{language}.json");

                string merged = shipped;
                if (File.Exists(onDiskPath)) {
                    try {
                        Dictionary<string, string> shippedKeys = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(shipped);
                        Dictionary<string, string> onDiskKeys = SimpleJson.SimpleJson.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(onDiskPath));
                        ReconcileKeys(shippedKeys, onDiskKeys);
                        merged = SimpleJson.SimpleJson.SerializeObject(onDiskKeys);
                    } catch (Exception e) {
                        // A hand-edited file that no longer parses gets replaced with the shipped copy rather
                        // than silently leaving the player with a broken (or missing) translation.
                        ModLogger.LogWarning($"Could not merge '{onDiskPath}' ({e.Message}); replacing it with the shipped localization.");
                        merged = shipped;
                    }
                }

                File.WriteAllText(onDiskPath, merged);
                localization.AddJsonFile(language, merged);
                ModLogger.LogDebug($"Added localization '{language}' from {embeddedResource}");
            }
        }

        // Resource names look like "<Assembly>.<Folder>.<Language>.json" but the folder nesting varies by
        // mod, so take the segment before the .json extension rather than a fixed index.
        private static string LanguageFromResourceName(string embeddedResource) {
            string withoutExtension = embeddedResource.Substring(0, embeddedResource.Length - ".json".Length);
            return withoutExtension.Split('.').LastOrDefault();
        }

        // Adds keys the mod newly ships and drops keys it no longer ships, leaving player edits intact.
        private static void ReconcileKeys(Dictionary<string, string> shipped, Dictionary<string, string> onDisk) {
            List<string> staleKeys = onDisk.Keys.Where(key => !shipped.ContainsKey(key)).ToList();
            foreach (KeyValuePair<string, string> entry in shipped) {
                if (onDisk.ContainsKey(entry.Key)) { continue; }
                ModLogger.LogDebug($"Adding missing localization key {entry.Key}");
                onDisk.Add(entry.Key, entry.Value);
            }
            if (staleKeys.Count > 0) {
                ModLogger.LogDebug($"Removing extra keys {string.Join(",", staleKeys.ToArray())}.");
                foreach (string key in staleKeys) { onDisk.Remove(key); }
            }
        }

        private static string ReadEmbeddedResourceFile(string filename, Assembly assembly) {
            using (Stream stream = assembly.GetManifestResourceStream(filename))
            using (StreamReader reader = new StreamReader(stream)) {
                return reader.ReadToEnd();
            }
        }
    }
}
