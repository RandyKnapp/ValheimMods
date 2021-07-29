using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ThunderKit.Common.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ThunderKit.Common.Package
{
    public static class PackageHelper
    {
        public static void GeneratePackageManifest(string packageName, string outputDir, string modName, string authorAlias, string modVersion, string description = null)
        {
            string unityVersion = Application.unityVersion.Substring(0, Application.unityVersion.LastIndexOf("."));
            var author = new Author
            {
                name = authorAlias,
            };
            var packageManifest = new PackageManagerManifest(author, packageName, ObjectNames.NicifyVariableName(modName), modVersion, unityVersion, description);
            var packageManifestJson = JsonUtility.ToJson(packageManifest);
            ScriptingSymbolManager.AddScriptingDefine(packageName);
            File.WriteAllText(Path.Combine(outputDir, "package.json"), packageManifestJson);
        }

        public static PackageManagerManifest GetPackageManagerManifest(string directory)
        {
            var packageJsonPath = Path.Combine(directory, "package.json");
            var json = File.ReadAllText(packageJsonPath);
            var pmm = JsonUtility.FromJson<PackageManagerManifest>(json);
            return pmm;
        }

        /// <summary>
        /// Generate a unity meta file for an assembly with ThunderKit managed Guid
        /// </summary>
        /// <param name="assemblyPath">Path to assembly to generate meta file for</param>
        /// <param name="metadataPath">Path to write meta file to</param>
        public static void WriteAssemblyMetaData(string assemblyPath, string metadataPath)
        {
            string guid = GetFileNameHash(assemblyPath);
            string metaData = DefaultAssemblyMetaData(guid);
            if (File.Exists(metadataPath)) File.Delete(metadataPath);
            File.WriteAllText(metadataPath, metaData);
        }

        /// <summary>
        /// Generate a unity meta file for an asset with ThunderKit managed Guid
        /// </summary>
        /// <param name="assemblyPath">Path to asset to generate meta file for</param>
        /// <param name="metadataPath">Path to write meta file to</param>
        public static void WriteAssetMetaData(string assetPath)
        {
            string guid = GetFileNameHash(assetPath);
            string metaData = DefaultAssemblyMetaData(guid);

            var metadataPath = $"{assetPath}.meta";
            if (File.Exists(metadataPath)) File.Delete(metadataPath);
            File.WriteAllText(metadataPath, metaData);

        }

        public static string GetFileNameHash(string assemblyPath)
        {
            using (var md5 = MD5.Create())
            {
                string shortName = Path.GetFileNameWithoutExtension(assemblyPath);
                byte[] shortNameBytes = Encoding.Default.GetBytes(shortName);
                var shortNameHash = md5.ComputeHash(shortNameBytes);
                var guid = new Guid(shortNameHash);
                var cleanedGuid = guid.ToString().ToLower().Replace("-", "");
                return cleanedGuid;
            }
        }

        public static string DefaultScriptableObjectMetaData(string guid) =>
$@"fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
";

        public static string DefaultAssemblyMetaData(string guid) =>
$@"fileFormatVersion: 2
guid: {guid}
PluginImporter:
  externalObjects: {{}}
  serializedVersion: 2
  iconMap: {{}}
  executionOrder: {{}}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 0
  isExplicitlyReferenced: 1
  validateReferences: 0
  platformData:
  - first:
      '': Any
    second:
      enabled: 0
      settings:
        Exclude Editor: 0
        Exclude Linux: 0
        Exclude Linux64: 0
        Exclude LinuxUniversal: 0
        Exclude OSXUniversal: 0
        Exclude Win: 0
        Exclude Win64: 0
  - first:
      Any: 
    second:
      enabled: 1
      settings: {{}}
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
        DefaultValueInitialized: true
        OS: AnyOS
  - first:
      Facebook: Win
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  - first:
      Facebook: Win64
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Linux
    second:
      enabled: 1
      settings:
        CPU: x86
  - first:
      Standalone: Linux64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  - first:
      Standalone: LinuxUniversal
    second:
      enabled: 1
      settings: {{}}
  - first:
      Standalone: OSXUniversal
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Win
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: AnyCPU
  - first:
      Windows Store Apps: WindowsStoreApps
    second:
      enabled: 0
      settings:
        CPU: AnyCPU
  userData: 
  assetBundleName: 
  assetBundleVariant: ";
    }
}