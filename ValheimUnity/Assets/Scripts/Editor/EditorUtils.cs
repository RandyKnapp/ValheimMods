using System.IO;
using UnityEngine;
using UnityEditor;

public class EditorUtils
{
    [MenuItem("Mod/Build Asset Bundles")]
    static void BuildAssetBundles()
    {
        string stageDirName = "AssetBundles";
        string projectRootDir = Path.Combine(Application.dataPath, "..");
        string stagePath = Path.Combine(projectRootDir, stageDirName);

        BuildPipeline.BuildAssetBundles(stagePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // A StandaloneWindows bundle carries no Metal shader programs, so on macOS every bundled
        // material renders pink. Build a second set for macOS; mods embed it next to the Windows one.
        // Requires the Mac Build Support module in the Unity install.
        string osxStagePath = Path.Combine(stagePath, "osx");
        Directory.CreateDirectory(osxStagePath);
        BuildPipeline.BuildAssetBundles(osxStagePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
    }

/*    public class MakeScriptableObject
    {
        [MenuItem("Assets/Create/Valheim/StatusEffect")]
        public static void CreateMyAsset()
        {
            StatusEffect asset = ScriptableObject.CreateInstance<StatusEffect>();

            AssetDatabase.CreateAsset(asset, "Assets/NewScripableObject.asset");
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
        }
    }*/
}