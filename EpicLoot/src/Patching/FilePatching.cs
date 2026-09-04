using BepInEx;
using Common;

using EpicLoot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Patching
{
    [Serializable]
    public enum PatchAction
    {
        None,           // Do nothing
        Add,            // Add the provided value to the selected object with the provided property name, if the property already exists, it's value is overwritten
        Overwrite,      // Replace the selected token's value with the provided value
        Remove,         // Remove the selected token from the array or object
        Append,         // Append the provided value to the end of the selected array
        AppendAll,      // Append the provided array to the end of the selected array.
        InsertBefore,   // Insert the provided value into the array containing the selected token, before the token
        InsertAfter,    // Insert the provided value into the array containing the selected token, after the token
        RemoveAll,      // Remove all elements of an array or all properties of an object
        Merge,          // Use property values in the provided object to add or overwrite property values on the selected object
        MultiAdd,       // Add the provided value to all defined values in MultiPropertyName
    }

    [Serializable]
    public class Patch
    {
        public int Priority = -1;
        public string Author = "";
        public string SourceFile = "";
        public string TargetFile = "";
        public string Path = "";
        public PatchAction Action = PatchAction.None;
        public bool Require;
        public string PropertyName = "";
        public JToken Value = null;
        public string[] MultiPropertyName = null;
    }

    [Serializable]
    public class PatchFile
    {
        public int Priority = 500;
        public string TargetFile = "";
        public string Author = "";
        public bool RequireAll = false;
        public List<Patch> Patches;
    }

    public static class FilePatching
    {
        public static string PatchesDirPath = GetPatchesDirectoryPath();
        public static List<string> ConfigFileNames = [
            "loottables",
            "magiceffects",
            "iteminfo",
            "enchantcosts",
            "itemnames",
            "itemsorter",
            "biomedata",
            "adventuredata",
            "legendaries",
            "abilities",
            "materialconversions",
            "enchantingupgrades",
            "shardstones",
            "shardstoneconversions"
        ];
        public static MultiValueDictionary<string, Patch> PatchesPerFile = new MultiValueDictionary<string, Patch>();

        /// <summary>
        /// Rebuilds every patched config file on disk. Returns the target names (no extension) whose
        /// files were rewritten, so the caller can pull exactly those back into the live config --
        /// the per-file watchers report the same writes, but only asynchronously.
        /// </summary>
        public static List<string> ReloadAndApplyAllPatches()
        {
            // Remember which targets HAD patches: a file whose last patch was just deleted must be
            // rebuilt from the embedded default, or it keeps the stale patched output forever.
            List<string> previousTargets = new List<string>(Keys);
            PatchesPerFile.Clear();
            LoadAllPatches();
            ApplyAllPatches();

            List<string> rewritten = new List<string>(Keys);

            foreach (string target in previousTargets)
            {
                if (PatchesPerFile.GetValues(target, true).Count == 0)
                {
                    EpicLoot.LogForce($"All patches targeting '{target}' were removed; restoring the unpatched default.");
                    string baseCfgFile = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), $"{target}.json");
                    ELConfig.CreateBaseConfigurations(baseCfgFile, $"{target}.json");
                    // Not in Keys any more (its patches are gone), so it cannot already be in the list.
                    rewritten.Add(target);
                }
            }

            return rewritten;
        }

        private static IEnumerable<string> Keys => PatchesPerFile.Keys;

        public static void LoadAllPatches()
        {
            try
            {
                PatchesDirPath = GetPatchesDirectoryPath();
            }
            catch (Exception e)
            {
                EpicLoot.LogWarning($"Unable to Get Patch Directory: {e.Message}");
                string debugPath = GetPatchesDirectoryPath(true);
                EpicLoot.LogWarning($"Attempted path is [{debugPath}]");
                return;
            }

            try
            {
                // If the folder does not exist, there are no patches
                if (string.IsNullOrEmpty(PatchesDirPath))
                {
                    return;
                }

                DirectoryInfo patchesFolder = new DirectoryInfo(PatchesDirPath);
                if (!patchesFolder.Exists)
                {
                    return;
                }

                ProcessPatchDirectory(patchesFolder);
            }
            catch (Exception e)
            {
                EpicLoot.LogWarning($"Unable to Get Patch Directory: {e.Message}");
                string debugPath = GetPatchesDirectoryPath(true);
                EpicLoot.LogWarning($"Attempted PatchesDirPath is [{PatchesDirPath}]");
                EpicLoot.LogWarning($"Attempted debugPath is [{debugPath}]");
            }
        }

        public static void ProcessPatchDirectory(DirectoryInfo dir)
        {
            FileInfo[] files = null;
            try
            {
                // Sort so ingestion (and thus equal-priority tie-breaking) is deterministic across platforms;
                // DirectoryInfo.GetFiles does not guarantee any particular order.
                files = dir.GetFiles("*.json").OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Error parsing patch directory ({dir.Name}): {e.Message}");
            }

            if (files != null)
            {
                foreach (FileInfo file in files)
                {
                    try
                    {
                        ProcessPatchFile(file);
                    }
                    catch (Exception e)
                    {
                        // Per-file isolation: one malformed patch file must not abort the rest of
                        // the directory (or, worse, the whole patch system).
                        EpicLoot.LogErrorForce($"Error processing patch file ({file.Name}): {e.Message}");
                    }
                }
            }

            DirectoryInfo[] subDirs = dir.GetDirectories().OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (DirectoryInfo subDir in subDirs)
            {
                try
                {
                    ProcessPatchDirectory(subDir);
                }
                catch (Exception e)
                {
                    EpicLoot.LogErrorForce($"Error processing patch directory ({subDir.Name}): {e.Message}");
                }
            }
        }

        public static List<string> ProcessPatchFile(FileInfo file)
        {
            string defaultTargetFile = "";
            // ConfigFileNames entries have no extension, but file.Name includes ".json"; compare without it
            // so a patch file named like a config (e.g. "loottables.json") infers its TargetFile.
            string fileNameNoExt = Path.GetFileNameWithoutExtension(file.Name);
            if (ConfigFileNames.Contains(fileNameNoExt))
            {
                defaultTargetFile = fileNameNoExt;
            }

            PatchFile patchFile = null;
            try
            {
                patchFile = JsonConvert.DeserializeObject<PatchFile>(File.ReadAllText(file.FullName));
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: {e.Message}");
                return null;
            }

            if (patchFile == null)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: unknown!");
                return null;
            }

            // Normalize away any ".json" so the file-name-derived default and an explicit TargetFile compare equal.
            string specifiedTargetFile = (patchFile.TargetFile ?? "").Replace(".json", "");

            if (!string.IsNullOrEmpty(specifiedTargetFile) && !string.IsNullOrEmpty(defaultTargetFile) &&
                specifiedTargetFile != defaultTargetFile)
            {
                EpicLoot.LogWarningForce($"TargetFile ({patchFile.TargetFile}) specified in patch file ({file.Name}) " +
                    $"does not match! If patch file name matches a config file name, TargetFile is unnecessary.");
            }

            if (!string.IsNullOrEmpty(specifiedTargetFile))
            {
                defaultTargetFile = specifiedTargetFile;
            }

            if (!string.IsNullOrEmpty(defaultTargetFile) && !ConfigFileNames.Contains(defaultTargetFile))
            {
                EpicLoot.LogErrorForce($"TargetFile ({defaultTargetFile}) specified in patch file ({file.Name}) " +
                    $"does not exist! {file.Name} will not be processed.");
                return null;
            }

            if (patchFile.Patches == null)
            {
                // A stray JSON in patches/ (a config backup, or a patch file missing its "Patches"
                // array) deserializes fine with a null list -- skip it instead of throwing, which
                // used to abort every remaining patch file and subdirectory.
                EpicLoot.LogErrorForce($"Patch file ({file.Name}) has no \"Patches\" array; skipping it.");
                return null;
            }

            bool requiresSpecifiedSourceFile = string.IsNullOrEmpty(defaultTargetFile);

            string author = string.IsNullOrEmpty(patchFile.Author) ? "<author>" : patchFile.Author;
            bool requireAll = patchFile.RequireAll;
            int defaultPriority = patchFile.Priority;
            List<string> filesWithNewPatches = new List<string>();

            foreach (Patch patch in patchFile.Patches)
            {
                EpicLoot.Log($"Patch: ({file.Name})\n  > Action: {patch.Action}\n  > " +
                    $"Path: {patch.Path}\n  > Value: {patch.Value}");

                patch.Require = requireAll || patch.Require;
                if (string.IsNullOrEmpty(patch.Author))
                {
                    patch.Author = author;
                }

                // Normalize away ".json" exactly like the file-level TargetFile above, so
                // "loottables.json" is accepted in both places.
                if (!string.IsNullOrEmpty(patch.TargetFile))
                {
                    patch.TargetFile = patch.TargetFile.Replace(".json", "");
                }

                if (string.IsNullOrEmpty(patch.TargetFile))
                {
                    if (requiresSpecifiedSourceFile)
                    {
                        EpicLoot.LogErrorForce($"Patch in file ({file.Name}) " +
                            $"requires a specified TargetFile!");
                        continue;
                    }

                    patch.TargetFile = defaultTargetFile;
                }
                else if (!ConfigFileNames.Contains(patch.TargetFile))
                {
                    EpicLoot.LogErrorForce($"Patch in file ({file.Name}) " +
                        $"has unknown specified source file ({patch.TargetFile})!");
                    continue;
                }

                if (patch.Priority < 0)
                {
                    patch.Priority = defaultPriority;
                }

                patch.SourceFile = file.Name;
                EpicLoot.Log($"Adding Patch from {patch.SourceFile} to file {patch.TargetFile} with {patch.Path}");
                PatchesPerFile.Add(patch.TargetFile, patch);
                // each patch section can add a different file, but we only need to actually refresh the file once.
                if (filesWithNewPatches.Contains(patch.TargetFile) == false)
                {
                    filesWithNewPatches.Add(patch.TargetFile);
                }
            }

            return filesWithNewPatches;
        }

        public static string GetPatchesDirectoryPath(bool debug = false)
        {
            string patchesFolderPath = Path.Combine(Paths.ConfigPath, "EpicLoot", "patches");
            
            if (debug)
            {
                return patchesFolderPath;
            }

            DirectoryInfo dirInfo = Directory.CreateDirectory(patchesFolderPath);

            return dirInfo.FullName;
        }

        public static string BuildPatchedConfig(string targetFile, JObject sourceJson)
        {
            // Higher Priority is applied first; since later patches overwrite earlier ones, the lowest-priority
            // patch has the final say (per the documented semantics). ThenBy makes equal-priority ties
            // deterministic by source file name instead of depending on filesystem enumeration order.
            List<Patch> patches = PatchesPerFile.GetValues(targetFile, true)
                .OrderByDescending(x => x.Priority)
                .ThenBy(x => x.SourceFile, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (Patch patch in patches)
            {
                try
                {
                    ApplyPatch(sourceJson, patch);
                }
                catch (Exception e)
                {
                    // Per-patch isolation: one throwing patch (bad JSONPath, wrong token type) used
                    // to discard every patch for this file -- including other mods' -- and leave the
                    // previous launch's output on disk.
                    EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) for " +
                        $"({patch.TargetFile}) threw and was skipped: {e.Message}");
                }
            }

            string output = sourceJson.ToString(Formatting.Indented);
            return output;
        }

        // Re-applies patches for every target file that has patches loaded (used by hot reload).
        public static void ApplyAllPatches()
        {
            foreach (KeyValuePair<string, List<Patch>> entry in PatchesPerFile)
            {
                LoadPatchedJSON(entry.Key);
            }

            LogAppliedPatchSummary();
        }

        // Always-on (not gated by the logging config) summary so bug reports reveal when the user is
        // actually running patched configs. Logs nothing when no patches are loaded.
        public static void LogAppliedPatchSummary()
        {
            int fileCount = PatchesPerFile.Count;
            int total = 0;
            foreach (KeyValuePair<string, List<Patch>> entry in PatchesPerFile)
            {
                total += entry.Value.Count;
            }

            if (total > 0)
            {
                EpicLoot.LogForce($"Applied {total} config patch(es) across {fileCount} config file(s).");
            }
        }

        internal static void LoadPatchedJSON(string filename)
        {
            string baseCfgFile = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), $"{filename}.json");
            List<Patch> patches = PatchesPerFile.GetValues(filename, true);

            // No patches target this file: refresh from the embedded default only if requested or missing,
            // otherwise leave the file alone so manual edits are respected.
            if (patches.Count == 0)
            {
                if (File.Exists(baseCfgFile) == false || ELConfig.AlwaysRefreshCoreConfigs.Value)
                {
                    ELConfig.CreateBaseConfigurations(baseCfgFile, $"{filename}.json");
                }

                return;
            }

            try
            {
                // Always build from the pristine embedded default (not the previously-written file) so that
                // Priority ordering is honored and repeated application (startup + hot reload) is idempotent
                // (no baking of prior results, no duplicated Appends).
                string embeddedConfig = EpicLoot.ReadEmbeddedResourceFile(
                    ELConfig.GetDefaultEmbeddedFileLocation($"{filename}.json"));
                JObject baseJson = JObject.Parse(embeddedConfig);
                string patchedString = BuildPatchedConfig(filename, baseJson);
                // Writing the file triggers the config file watcher, which reloads the config in-game.
                File.WriteAllText(baseCfgFile, patchedString);

                EpicLoot.Log($"Loaded and applied patches for {filename}.json");
            }
            catch (Exception e)
            {
                EpicLoot.LogWarningForce($"Applying patches for {filename}.json failed!\n {e}");
            }
        }

        public static void ApplyPatch(JObject json, Patch patch)
        {
            List<JToken> selectedTokens = json.SelectTokens(patch.Path).ToList();
            // Removals that have already happened are allowed to re-run without warnings, since they are no-ops
            if (patch.Require && selectedTokens.Count == 0 && patch.Action != PatchAction.Remove)
            {
                EpicLoot.LogErrorForce($"Required Patch ({patch.SourceFile}) path ({patch.Path}) " +
                    $"failed to select any tokens in target file ({patch.TargetFile})!");
                return;
            }

            foreach (JToken token in selectedTokens)
            {
                switch (patch.Action)
                {
                    case PatchAction.Add: ApplyPatch_Add(token, patch); break;
                    case PatchAction.Overwrite: ApplyPatch_Overwrite(token, patch); break;
                    case PatchAction.Remove: ApplyPatch_Remove(token, patch); break;
                    case PatchAction.Append: ApplyPatch_Append(token, patch); break;
                    case PatchAction.AppendAll: ApplyPatch_Append(token, patch, true); break;
                    case PatchAction.InsertBefore: ApplyPatch_Insert(token, patch, false); break;
                    case PatchAction.InsertAfter: ApplyPatch_Insert(token, patch, true); break;
                    case PatchAction.RemoveAll: ApplyPatch_RemoveAll(token, patch); break;
                    case PatchAction.Merge: ApplyPatch_Merge(token, patch); break;
                    case PatchAction.MultiAdd: ApplyPatch_MultiAdd(token, patch); break;
                    default: break;
                }
            }
        }

        public static void ApplyPatch_MultiAdd(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'MultiAdd' " +
                    $"but has not supplied a Value for the added value! This patch will be ignored!");
                return;
            }

            if (patch.MultiPropertyName == null || patch.MultiPropertyName.Length == 0)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'MultiAdd' " +
                    $"but has not supplied MultiPropertyName! This patch will be ignored!");
                return;
            }

            if (token.Type != JTokenType.Object)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'MultiAdd' " +
                    $"but has selected a token that is not a json Object! This patch will be ignored!");
                return;
            }

            foreach (string item in patch.MultiPropertyName)
            {
                Patch_Add(token, item, patch.Value);
            }
        }

        public static void ApplyPatch_Add(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' " +
                    $"but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (string.IsNullOrEmpty(patch.PropertyName))
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' " +
                    $"but has not supplied a PropertyName for the added value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                Patch_Add(token, patch.PropertyName, patch.Value);
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' " +
                    $"but has selected a token that is not a json Object! This patch will be ignored!");
            }
        }

        internal static void Patch_Add(JToken token, string property, JToken value)
        {
            JObject jObject = (JObject)token;
            if (jObject.ContainsKey(property) && jObject.Property(property) is JProperty jProperty)
            {
                EpicLoot.LogWarning($"Patch has action 'Add' but a property with the name ({property}) already exists! " +
                    $"The property's value will be overwritten");
                jProperty.Value = value;
            }
            else
            {
                jObject.Add(property, value);
            }
        }

        public static void ApplyPatch_Overwrite(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' " +
                    $"but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Property)
            {
                ((JProperty)token).Value = patch.Value;
            }
            else if (token.Parent?.Type == JTokenType.Property)
            {
                ((JProperty)token.Parent).Value = patch.Value;
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' " +
                    $"but did not select a json Object Property or Property Value! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Remove(JToken token, Patch patch)
        {
            if (patch.Value != null)
            {
                EpicLoot.LogWarning($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Remove' " +
                    $"but has supplied an unnecessary json Value. (This patch will still be processed)");
            }

            token.Remove();
        }

        public static void ApplyPatch_Append(JToken token, Patch patch, bool appendAll = false)
        {
            string actionName = appendAll ? "AppendAll" : "Append";

            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Array)
            {
                if (appendAll)
                {
                    if (patch.Value.Type == JTokenType.Array)
                    {
                        JsonMergeSettings mergeSettings = new JsonMergeSettings
                        {
                            // Do not create duplicates when appending arrays
                            MergeArrayHandling = MergeArrayHandling.Union,
                            MergeNullValueHandling = MergeNullValueHandling.Ignore
                        };
                        ((JArray)token).Merge(patch.Value, mergeSettings);
                    }
                    else
                    {
                        EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'AppendAll' " +
                            $"but has provided a value in the source file that is not a json Array!");
                    }
                }
                else
                {
                    ((JArray)token).Add(patch.Value);
                }

            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action {actionName} " +
                    $"but has selected a token in the target file that is not a json Array!");
            }
        }

        public static void ApplyPatch_Insert(JToken token, Patch patch, bool after)
        {
            string actionName = $"Insert{(after ? "After" : "Before")}";
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            // SelectTokens("$.foo") selects the property's VALUE; its parent is the JProperty.
            // Unwrap to the property (same as ApplyPatch_Overwrite) so inserting relative to an
            // object member works -- this case previously fell through both branches silently.
            if (token.Parent?.Type == JTokenType.Property)
            {
                token = token.Parent;
            }

            JContainer parent = token.Parent;
            if (parent == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but the parent of the selected token is not a container! This patch will be ignored!");
                return;
            }

            if (parent.Type == JTokenType.Array)
            {
                if (after)
                {
                    token.AddAfterSelf(patch.Value);
                }
                else
                {
                    token.AddBeforeSelf(patch.Value);
                }
            }
            else if (parent.Type == JTokenType.Object)
            {
                if (string.IsNullOrEmpty(patch.PropertyName))
                {
                    EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' and " +
                        $"has selected a property of a json Object, but not provided a PropertyName! This patch will be ignored!");
                    return;
                }

                if (after)
                {
                    token.AddAfterSelf(new JProperty(patch.PropertyName, patch.Value));
                }
                else
                {
                    token.AddBeforeSelf(new JProperty(patch.PropertyName, patch.Value));
                }
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but the selected token's parent is neither an Array nor an Object! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_RemoveAll(JToken token, Patch patch)
        {
            const string actionName = "RemoveAll";
            if (patch.Value != null)
            {
                EpicLoot.LogWarning($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but has supplied a json Value! (This patch will still be processed)");
            }

            if (token.Type == JTokenType.Array)
            {
                ((JArray)token).RemoveAll();
            }
            else if (token.Type == JTokenType.Object)
            {
                ((JObject)token).RemoveAll();
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' " +
                    $"but selected token is not an Array or Object! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Merge(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' " +
                    $"but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (patch.Value.Type != JTokenType.Object)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' " +
                    $"but has supplied a json Value that is not an Object! This patch will be ignored!");
                return;
            }

            if (token.Type != JTokenType.Object)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Merge' " +
                    $"but has selected a token that is not a json Object! This patch will be ignored!");
                return;
            }

            JObject baseObject = ((JObject)token);
            JObject partialObject = ((JObject)patch.Value);

            MergeObject(baseObject, partialObject);
        }

        private static void MergeObject(JObject baseObject, JObject partialObject)
        {
            foreach (JProperty partialProperty in partialObject.Properties())
            {
                if (baseObject.ContainsKey(partialProperty.Name) && baseObject.Property(partialProperty.Name) is JProperty baseProperty)
                {
                    if (baseProperty.Value.Type == JTokenType.Object && partialProperty.Value.Type == JTokenType.Object)
                    {
                        MergeObject((JObject)baseProperty.Value, (JObject)partialProperty.Value);
                    }
                    else
                    {
                        baseProperty.Value = partialProperty.Value;
                    }
                }
                else
                {
                    baseObject.Add(partialProperty.Name, partialProperty.Value);
                }
            }
        }
    }
}
