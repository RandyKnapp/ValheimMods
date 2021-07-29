using System.Diagnostics;
using System.IO;
using System.Linq;
using ThunderKit.Common;
using ThunderKit.Core.Data;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ThunderKit.Core.Config
{
    using static ThunderKit.Common.PathExtensions;
    internal struct SwapPair
    {
        public string newFile;
        public string originalFile;

        public SwapPair(string newFile, string originalFile)
        {
            this.newFile = newFile;
            this.originalFile = originalFile;
        }
    }

    public class SetupDebugBuild
    {
        private const string playerConnectionDebug1 = "player-connection-debug=1";

        [MenuItem(Constants.ThunderKitMenuRoot + "Setup Debug Build", priority = Constants.ThunderKitMenuPriority - 1)]
        public static void Execute()
        {
            var settings = ThunderKitSettings.GetOrCreateSettings<ThunderKitSettings>();

            var gamePath = settings.GamePath;
            var gameName = Path.GetFileNameWithoutExtension(settings.GameExecutable);
            var gameMonoPath = Path.Combine(gamePath, $"MonoBleedingEdge");
            var gameDataPath = Path.Combine(gamePath, $"{gameName}_Data");
            var gameManagedPath = Path.Combine(gameDataPath, "Managed");
            var gameBootConfigFile = Path.Combine(gameDataPath, "boot.config");

            var editorPath = Path.GetDirectoryName(EditorApplication.applicationPath);
            var windowsStandalonePath = Combine(editorPath, "Data", "PlaybackEngines", "windowsstandalonesupport");

            var gamePlayer = Path.Combine(gamePath, $"{gameName}.exe");

            var monoString = settings.Is64Bit ? "win64_development_mono" : "win32_development_mono";
            var crashHandlerFile = settings.Is64Bit ? "UnityCrashHandler64.exe" : "UnityCrashHandler32.exe";
            var playerPdbFile = settings.Is64Bit ? "UnityPlayer_Win64_development_mono_x64.pdb" : "UnityPlayer_Win32_development_mono_x86.pdb";
            var playerReleasePdb = settings.Is64Bit ? "WindowsPlayer_Release_mono_x64.pdb" : "WindowsPlayer_Release_mono_x86.pdb";

            var bitVersionPath = Combine(windowsStandalonePath, "Variations", monoString);
            var monoBleedingEdgePath = Path.Combine(bitVersionPath, "MonoBleedingEdge");
            var dataManagedPath = Combine(bitVersionPath, "Data", "Managed");
            var winPlayer = Path.Combine(bitVersionPath, "WindowsPlayer.exe");


            var crashHandler = GetSwapPair(bitVersionPath, gamePath, crashHandlerFile);
            var player = GetSwapPair(bitVersionPath, gamePath, "UnityPlayer.dll");
            var playerLib = GetSwapPair(bitVersionPath, gamePath, "UnityPlayer.dll.lib");
            var playerPdb = GetSwapPair(bitVersionPath, gamePath, playerPdbFile);
            var releasePdb = GetSwapPair(bitVersionPath, gamePath, playerReleasePdb);
            var winPix = GetSwapPair(bitVersionPath, gamePath, "WinPixEventRuntime.dll");

            var editorVersion = FileVersionInfo.GetVersionInfo(winPlayer);
            var gameVersion = FileVersionInfo.GetVersionInfo(gamePlayer);

            if (!editorVersion.Equals(editorVersion))
            {
                Debug.LogError($"Unity Editor Version: {editorVersion} does not match {settings.GameExecutable} Unity version: {gameVersion}");
                return;
            }

            Overwrite(new SwapPair { newFile = winPlayer, originalFile = gamePlayer });
            Overwrite(crashHandler);
            Overwrite(player);
            Overwrite(playerLib);
            Overwrite(playerPdb);
            Overwrite(releasePdb);
            if (File.Exists(winPix.newFile))
                Overwrite(winPix);

            CopyFolder(monoBleedingEdgePath, gameMonoPath);
            CopyFolder(dataManagedPath, gameManagedPath);

            if (File.Exists(gameBootConfigFile))
            {
                bool foundConnectionDebug = false;
                using (var sr = File.OpenText(gameBootConfigFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains(playerConnectionDebug1))
                        {
                            foundConnectionDebug = true;
                            break;
                        }
                    }

                }

                if (!foundConnectionDebug)
                    File.AppendAllText(gameBootConfigFile, playerConnectionDebug1);
            }
            else
                File.WriteAllText(gameBootConfigFile, playerConnectionDebug1);
        }

        private static SwapPair GetSwapPair(string sourceRoot, string destRoot, string fileName)
        {
            return new SwapPair { newFile = Path.Combine(sourceRoot, fileName), originalFile = Path.Combine(destRoot, fileName) };
        }

        private static void Overwrite(SwapPair swapPair) => Overwrite(swapPair.newFile, swapPair.originalFile);
        private static void Overwrite(string newFile, string originalFile)
        {
            if (File.Exists(originalFile)) File.Delete(originalFile);
            File.Copy(newFile, originalFile);
        }

        private static void CopyFolder(string sourcePath, string destinationPath)
        {
            foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(destinationPath, dir.Substring(sourcePath.Length + 1)));

            foreach (var fileName in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var original = Path.Combine(destinationPath, fileName.Substring(sourcePath.Length + 1));
                Overwrite(fileName, original);
            }
        }
    }

}