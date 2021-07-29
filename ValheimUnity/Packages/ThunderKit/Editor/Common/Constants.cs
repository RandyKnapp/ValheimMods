using System.IO;
using UnityEditor;

namespace ThunderKit.Common
{
    public static class Constants
    {
        public const string ThunderKit = nameof(ThunderKit);

        public const int ThunderKitMenuPriority = 18;
        public const string ThunderKitContextRoot = "Assets/ThunderKit/";
        public const string ThunderKitMenuRoot = "Tools/ThunderKit/";
        public const string ThunderKitSettingsRoot = "Assets/ThunderKitSettings/";

        public static readonly string TempDir = PathExtensions.Combine(Directory.GetCurrentDirectory(), "Temp", ThunderKit);
        public static readonly string Packages = "Packages";
        public static readonly string[] AssetDatabaseFindFolders = new[] { "Packages", "Assets" };
        public static readonly string ThunderKitPackageName = "com.passivepicasso.thunderkit";

        [InitializeOnLoadMethod]
        static void SetupTempDir()
        {
            Directory.CreateDirectory(TempDir);
        }
    }
}