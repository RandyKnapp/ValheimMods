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
    }
}