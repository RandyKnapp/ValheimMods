using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        InsertBefore,   // Insert the provided value into the array containing the selected token, before the token
        InsertAfter,    // Insert the provided value into the array containing the selected token, after the token
        RemoveAll,      // Remove all elements of an array or all properties of an object
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
        public static string PatchesDirPath;
        public static List<string> ConfigFileNames = new List<string>();
        public static MultiValueDictionary<string, Patch> PatchesPerFile = new MultiValueDictionary<string, Patch>();

        public static void LoadAllPatches()
        {
            PatchesDirPath = GetPatchesDirectoryPath();

            // If the folder does not exist, there are no patches
            if (string.IsNullOrEmpty(PatchesDirPath))
                return;

            var patchesFolder = new DirectoryInfo(PatchesDirPath);
            if (!patchesFolder.Exists)
                return;

            var pluginFolder = patchesFolder.Parent;
            GetAllConfigFileNames(pluginFolder);
            ProcessPatchDirectory(patchesFolder);
        }

        public static void RemoveFilePatches(string fileName, string patchFile)
        {
            PatchesPerFile.GetValues(fileName,true).RemoveAll(y => y.SourceFile.Equals(patchFile));
        }

        public static void GetAllConfigFileNames(DirectoryInfo pluginFolder)
        {
            ConfigFileNames.Clear();

            FileInfo[] files = null;
            try
            {
                files = pluginFolder.GetFiles("*.json");
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Error parsing patch directory ({pluginFolder.Name}): {e.Message}");
            }

            if (files == null)
                return;

            foreach (var file in files)
            {
                EpicLoot.LogWarning($"ConfigFile: {file.Name}");
                ConfigFileNames.Add(file.Name);
            }
        }

        public static void ProcessPatchDirectory(DirectoryInfo dir)
        {
            FileInfo[] files = null;
            try
            {
                files = dir.GetFiles("*.json");
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Error parsing patch directory ({dir.Name}): {e.Message}");
            }

            if (files != null)
            {
                foreach (var file in files)
                {
                    ProcessPatchFile(file);
                }
            }

            var subDirs = dir.GetDirectories();
            foreach (var subDir in subDirs)
            {
                ProcessPatchDirectory(subDir);
            }
        }

        public static void ProcessPatchFile(FileInfo file)
        {
            var defaultTargetFile = "";
            if (ConfigFileNames.Contains(file.Name))
                defaultTargetFile = file.Name;

            PatchFile patchFile = null;
            try
            {
                patchFile = JsonConvert.DeserializeObject<PatchFile>(File.ReadAllText(file.FullName));
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: {e.Message}");
                return;
            }

            if (patchFile == null)
            {
                EpicLoot.LogErrorForce($"Error parsing patch file ({file.Name})! Error: unknown!");
                return;
            }

            if (!string.IsNullOrEmpty(patchFile.TargetFile) && !string.IsNullOrEmpty(defaultTargetFile) && patchFile.TargetFile != defaultTargetFile)
                EpicLoot.LogWarningForce($"TargetFile ({patchFile.TargetFile}) specified in patch file ({file.Name}) does not match! If patch file name matches a config file name, TargetFile is unnecessary.");

            if (!string.IsNullOrEmpty(patchFile.TargetFile))
                defaultTargetFile = patchFile.TargetFile;

            if (!string.IsNullOrEmpty(defaultTargetFile) && !ConfigFileNames.Contains(defaultTargetFile))
            {
                EpicLoot.LogErrorForce($"TargetFile ({defaultTargetFile}) specified in patch file ({file.Name}) does not exist! {file.Name} will not be processed.");
                return;
            }

            var requiresSpecifiedSourceFile = string.IsNullOrEmpty(defaultTargetFile);

            var author = string.IsNullOrEmpty(patchFile.Author) ? "<author>" : patchFile.Author;
            var requireAll = patchFile.RequireAll;
            var defaultPriority = patchFile.Priority;

            for (var index = 0; index < patchFile.Patches.Count; index++)
            {
                var patch = patchFile.Patches[index];
                EpicLoot.Log($"Patch: ({file.Name}, {index})\n  > Action: {patch.Action}\n  > Path: {patch.Path}\n  > Value: {patch.Value}");

                patch.Require = requireAll || patch.Require;
                if (string.IsNullOrEmpty(patch.Author))
                    patch.Author = author;
                if (string.IsNullOrEmpty(patch.TargetFile))
                {
                    if (requiresSpecifiedSourceFile)
                    {
                        EpicLoot.LogErrorForce($"Patch ({index}) in file ({file.Name}) requires a specified TargetFile!");
                        continue;
                    }

                    patch.TargetFile = defaultTargetFile;
                }
                else if (!ConfigFileNames.Contains(patch.TargetFile))
                {
                    EpicLoot.LogErrorForce($"Patch ({index}) in file ({file.Name}) has unknown specified source file ({patch.TargetFile})!");
                    continue;
                }

                if (patch.Priority < 0)
                    patch.Priority = defaultPriority;

                patch.SourceFile = file.Name;
                EpicLoot.Log($"Adding Patch from {patch.SourceFile} to file {patch.TargetFile} with {patch.Path}");
                PatchesPerFile.Add(patch.TargetFile, patch);
            }
        }

        public static string GetPatchesDirectoryPath()
        {
            var patchesFolderPath = Path.Combine(Paths.PluginPath, "EpicLoot", "patches");
            if (!Directory.Exists(patchesFolderPath))
            {
                var assembly = typeof(EpicLoot).Assembly;
                patchesFolderPath = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, "patches");
                if (!Directory.Exists(patchesFolderPath))
                {
                    Directory.CreateDirectory(patchesFolderPath);
                }
            }

            return patchesFolderPath;
        }

        public static string ProcessConfigFile(string fileName, string fileText)
        {
            var patches = PatchesPerFile.GetValues(fileName, true).OrderByDescending(x => x.Priority).ToList();
            if (patches.Count == 0)
                return fileText;

            JObject json = null;
            try
            {
                json = JObject.Parse(fileText);
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Error parsing config file ({fileName})! Error: {e.Message}");
                return string.Empty;
            }

            foreach (var patch in patches)
            {
                ApplyPatch(json, patch);
            }

            var output = json.ToString(EpicLoot.OutputPatchedConfigFiles.Value ? Formatting.Indented : Formatting.None);
            return output;
        }

        public static void ApplyPatch(JObject json, Patch patch)
        {
            var selectedTokens = json.SelectTokens(patch.Path).ToList();
            if (patch.Require && selectedTokens.Count == 0)
            {
                EpicLoot.LogErrorForce($"Required Patch ({patch.SourceFile}) path ({patch.Path}) failed to select any tokens in target file ({patch.TargetFile})!");
                return;
            }

            foreach (var token in selectedTokens)
            {
                switch (patch.Action)
                {
                    case PatchAction.Add:           ApplyPatch_Add(token, patch);           break;
                    case PatchAction.Overwrite:     ApplyPatch_Overwrite(token, patch);     break;
                    case PatchAction.Remove:        ApplyPatch_Remove(token, patch);        break;
                    case PatchAction.Append:        ApplyPatch_Append(token, patch);        break;
                    case PatchAction.InsertBefore:  ApplyPatch_Insert(token, patch, false); break;
                    case PatchAction.InsertAfter:   ApplyPatch_Insert(token, patch, true);  break;
                    case PatchAction.RemoveAll:     ApplyPatch_RemoveAll(token, patch);     break;
                    default:                                                                break;
                }
            }
        }

        public static void ApplyPatch_Add(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (string.IsNullOrEmpty(patch.PropertyName))
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has not supplied a PropertyName for the added value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Object)
            {
                var jObject = ((JObject)token);
                if (jObject.ContainsKey(patch.PropertyName) && jObject.Property(patch.PropertyName) is JProperty jProperty)
                {
                    EpicLoot.LogWarningForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but a property with the name ({patch.PropertyName}) already exists! The property's value will be overwritten");
                    jProperty.Value = patch.Value;
                }
                else
                {
                    jObject.Add(patch.PropertyName, patch.Value);
                }
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Add' but has selected a token that is not a json Object! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Overwrite(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' but has not supplied a json Value! This patch will be ignored!");
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
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Overwrite' but did not select a json Object Property or Property Value! This patch will be ignored!");
            }
        }

        public static void ApplyPatch_Remove(JToken token, Patch patch)
        {
            if (patch.Value != null)
            {
                EpicLoot.LogWarningForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Remove' but has supplied a json Value. (This patch will still be processed)");
            }

            token.Remove();
        }

        public static void ApplyPatch_Append(JToken token, Patch patch)
        {
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Append' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            if (token.Type == JTokenType.Array)
            {
                ((JArray)token).Add(patch.Value);
            }
            else
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action 'Append' but has selected a token in the target file that is not a json Array!");
            }
        }

        public static void ApplyPatch_Insert(JToken token, Patch patch, bool after)
        {
            var actionName = $"Insert{(after ? "After" : "Before")}";
            if (patch.Value == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but has not supplied a json Value! This patch will be ignored!");
                return;
            }

            var parent = token.Parent;
            if (parent == null)
            {
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but the parent of the selected token is not a container! This patch will be ignored!");
                return;
            }

            if (parent.Type == JTokenType.Array)
            {
                if (after)
                    token.AddAfterSelf(patch.Value);
                else
                    token.AddBeforeSelf(patch.Value);
            }
            else if (parent.Type == JTokenType.Object)
            {
                if (string.IsNullOrEmpty(patch.PropertyName))
                {
                    EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' and has selected a property of a json Object, but not provided a PropertyName! This patch will be ignored!");
                    return;
                }

                if (after)
                    token.AddAfterSelf(new JProperty(patch.PropertyName, patch.Value));
                else
                    token.AddBeforeSelf(new JProperty(patch.PropertyName, patch.Value));
            }
        }

        public static void ApplyPatch_RemoveAll(JToken token, Patch patch)
        {
            const string actionName = "RemoveAll";
            if (patch.Value != null)
            {
                EpicLoot.LogWarningForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but has supplied a json Value! (This patch will still be processed)");
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
                EpicLoot.LogErrorForce($"Patch ({patch.SourceFile}, {patch.Path}) has action '{actionName}' but selected token is not an Array or Object! This patch will be ignored!");
            }
        }
    }
}
