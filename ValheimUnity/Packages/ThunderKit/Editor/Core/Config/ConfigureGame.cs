using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ThunderKit.Common.Package;
using ThunderKit.Core.Data;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ThunderKit.Core.Config
{
    using static ThunderKit.Common.PathExtensions;
    public class ConfigureGame
    {
        public static void Configure()
        {
            var settings = ThunderKitSettings.GetOrCreateSettings<ThunderKitSettings>();

            LoadGame(settings);

            if (string.IsNullOrEmpty(settings.GamePath) || string.IsNullOrEmpty(settings.GameExecutable)) return;

            SetBitness(settings);
            EditorUtility.SetDirty(settings);

            if (!CheckUnityVersion(settings)) return;

            var packageName = Path.GetFileNameWithoutExtension(settings.GameExecutable);
            AssertDestinations(packageName);

            GetReferences(packageName, settings);
            EditorUtility.SetDirty(settings);

            SetupPackageManifest(settings, packageName);

            AssetDatabase.Refresh();
        }

        private static void SetupPackageManifest(ThunderKitSettings settings, string packageName)
        {
            var name = packageName.ToLower().Split(' ').Aggregate((a, b) => $"{a}{b}");
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Path.Combine(settings.GamePath, settings.GameExecutable));
            var unityVersion = new Version(fileVersionInfo.FileVersion);
            var author = new Author
            {
                name = fileVersionInfo.CompanyName,
            };
            var packageManifest = new PackageManagerManifest(author, name, packageName, "1.0.0", $"{unityVersion.Major}.{unityVersion.Minor}", $"Imported Assets from game {packageName}");
            var packageManifestJson = JsonUtility.ToJson(packageManifest);
            File.WriteAllText(Combine("Packages", packageName, "package.json"), packageManifestJson);
        }

        private static void AssertDestinations(string packageName)
        {
            var destinationFolder = Path.Combine("Packages", packageName);
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);

            destinationFolder = Combine("Packages", packageName, "plugins");
            if (!Directory.Exists(destinationFolder))
                Directory.CreateDirectory(destinationFolder);
        }

        private static void LoadGame(ThunderKitSettings settings)
        {
            string currentDir = Directory.GetCurrentDirectory();
            var foundExecutable = !string.IsNullOrEmpty(settings.GamePath) 
                               && Directory.GetFiles(settings.GamePath ?? currentDir, Path.GetFileName(settings.GameExecutable)).Any();

            while (!foundExecutable)
            {
                string path = EditorUtility.OpenFilePanel("Open Game Executable", currentDir, "exe");
                if (string.IsNullOrEmpty(path)) return;
                settings.GameExecutable = Path.GetFileName(path);
                settings.GamePath = Path.GetDirectoryName(path);
                foundExecutable = Directory.GetFiles(settings.GamePath, settings.GameExecutable).Any();
            }
            EditorUtility.SetDirty(settings);
        }

        private static bool CheckUnityVersion(ThunderKitSettings settings)
        {
            var regs = new Regex("(\\d\\d\\d\\d\\.\\d+\\.\\d+).*");

            var unityVersion = regs.Replace(Application.unityVersion, match => match.Groups[1].Value);

            var playerVersion = FileVersionInfo.GetVersionInfo(Path.Combine(settings.GamePath, settings.GameExecutable)).ProductVersion;
            playerVersion = regs.Replace(playerVersion, match => match.Groups[1].Value);

            var versionMatch = unityVersion.Equals(playerVersion);
            Debug.Log($"Unity Editor version ({unityVersion}), Unity Player version ({playerVersion}){(versionMatch ? "" : ", aborting setup.\r\n\t Make sure you're using the same version of the Unity Editor as the Unity Player for the game.")}");
            return versionMatch;
        }

        private static void GetReferences(string packageName, ThunderKitSettings settings)
        {
            Debug.Log("Acquiring references");
            var blackList = AppDomain.CurrentDomain.GetAssemblies()
#if NET_4_6
                .Where(asm => !asm.IsDynamic)
#else 
                .Where(asm =>
                {
                    if (asm.ManifestModule is System.Reflection.Emit.ModuleBuilder mb)
                        return !mb.IsTransient();

                    return true;
                })
#endif
                .Select(asm =>
                {
                    try
                    {
                        return Path.GetFileName(asm.Location);
                    }
                    catch
                    {
                        return string.Empty;
                    }
                })
                .ToArray();

            var managedPath = Combine(settings.GamePath, $"{Path.GetFileNameWithoutExtension(settings.GameExecutable)}_Data", "Managed");
            var pluginsPath = Combine(settings.GamePath, $"{Path.GetFileNameWithoutExtension(settings.GameExecutable)}_Data", "Plugins");
            var packagePath = Path.Combine("Packages", packageName);
            var packagePluginsPath = Path.Combine(packagePath, "plugins");

            var managedAssemblies = Directory.GetFiles(managedPath, "*.dll");
            var plugins = Directory.GetFiles(pluginsPath, "*.dll");

            GetReferences(packagePath, managedAssemblies, blackList);
            GetReferences(packagePluginsPath, plugins, Enumerable.Empty<string>());
        }

        private static void GetReferences(string destinationFolder, IEnumerable<string> assemblies, IEnumerable<string> blackList)
        {
            foreach (var assemblyPath in assemblies)
            {
                if (blackList.Any(asm => asm.Equals(Path.GetFileName(assemblyPath)))) continue;

                var destinationFile = Path.Combine(destinationFolder, Path.GetFileName(assemblyPath));

                var destinationMetaData = Path.Combine(destinationFolder, $"{Path.GetFileName(assemblyPath)}.meta");

                if (File.Exists(destinationFile)) File.Delete(destinationFile);
                File.Copy(assemblyPath, destinationFile);

                PackageHelper.WriteAssemblyMetaData(assemblyPath, destinationMetaData);
            }
        }


        public static void SetBitness(ThunderKitSettings settings)
        {
            var assembly = Path.Combine(settings.GamePath, settings.GameExecutable);
            using (var stream = File.OpenRead(assembly))
            using (var binStream = new BinaryReader(stream))
            {
                stream.Seek(0x3C, SeekOrigin.Begin);
                if (binStream.PeekChar() != -1)
                {
                    var e_lfanew = binStream.ReadInt32();
                    stream.Seek(e_lfanew + 0x4, SeekOrigin.Begin);
                    var cpuType = binStream.ReadUInt16();
                    if (cpuType == 0x8664)
                    {
                        settings.Is64Bit = true;
                        return;
                    }
                }
            }
            settings.Is64Bit = false;
        }
    }
}
