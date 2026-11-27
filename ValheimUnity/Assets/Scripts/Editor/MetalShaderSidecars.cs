using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Builds "<bundle>_metal" into AssetBundles/osx for every bundle whose materials use shaders the bundle itself
// carries: the same shaders compiled for Metal, and nothing else.
//
// The bundles are built for StandaloneWindows, so their shaders hold D3D11, OpenGLCore and Vulkan programs only.
// Unity 6 renders through Metal alone on macOS, so every bundled material rendered pink there, after "Desired
// shader compiler platform 14 is not available in shader blob". Everything else in a Windows bundle loads fine on
// a Mac. A second macOS build of each bundle would fix it too, but would double EpicLoot.dll (about 42 MB) for
// half a megabyte of Metal programs. The mods embed these beside their bundles, and Common's MetalShaderSidecar
// moves each material onto the Metal shader of the same name when the game runs on Metal.
//
// The shaders get in through carriers: one material per shader and keyword set the bundle's materials use,
// with no textures. Built-in shaders (Standard, Legacy Shaders/Particles/*) cannot be named in an
// AssetBundleBuild, and Unity only copies them into a bundle that references them. And Unity drops every
// shader_feature variant no material in the build enables, so a bare shader would ship its base variant only
// (Standard's _NORMALMAP and _EMISSION, TMP's OUTLINE_ON and UNDERLAY_ON).
public static class MetalShaderSidecars
{
    const string Suffix = "_metal";
    const string OutputDirName = "osx";
    const string CarrierRoot = "Assets/_MetalShaderCarriers";

    // Ships with every player, so the game already has these for Metal.
    const string DefaultResourcesPath = "Library/unity default resources";

    // Jotunn swaps a JVLmock_ shader for the game's own when the prefab is registered with fixReference.
    const string MockPrefix = "JVLmock_";

    // Builds from the asset database, not from the Windows bundles, so it does not need them to be current.
    public static void Build(string projectRootDir, string stagePath)
    {
        string outputPath = Path.Combine(stagePath, OutputDirName);
        BuildRecord.Delete(outputPath);

        AssetBundleManifest manifest;
        try
        {
            DeleteCarriers();

            var builds = new List<AssetBundleBuild>();
            foreach (string bundleName in AssetDatabase.GetAllAssetBundleNames())
            {
                string[] carriers = CreateCarriers(bundleName);
                if (carriers.Length > 0)
                {
                    builds.Add(new AssetBundleBuild { assetBundleName = bundleName + Suffix, assetNames = carriers });
                }
            }

            if (builds.Count == 0)
            {
                Debug.Log("No bundle uses a shader it carries itself, so no Metal shader sidecars were built.");
                return;
            }

            Directory.CreateDirectory(outputPath);
            manifest = BuildPipeline.BuildAssetBundles(outputPath, builds.ToArray(),
                BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneOSX);
        }
        finally
        {
            DeleteCarriers();
        }

        if (manifest == null)
        {
            Debug.LogError("The Metal shader sidecar build failed (it needs Mac Build Support in this Unity install), " +
                "so no build record was written for " + outputPath + ".");
            return;
        }

        string[] built = manifest.GetAllAssetBundles();
        DeleteStaleSidecars(outputPath, built);
        Debug.Log("Built Metal shader sidecars: " + string.Join(", ", built));

        // After DeleteCarriers, so the record's list of uncommitted files does not name them.
        BuildRecord.Write(projectRootDir, outputPath, built);
    }

    // Saves one carrier per distinct shader, keyword set and instancing flag among the bundle's materials, and
    // returns their paths. Instancing is part of the key because the "Strip Unused" instancing setting keeps
    // instancing variants only for shaders that some material in the build enables it on.
    static string[] CreateCarriers(string bundleName)
    {
        var roots = new List<UnityEngine.Object>();
        foreach (string path in AssetDatabase.GetAssetPathsFromAssetBundle(bundleName))
        {
            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                roots.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).Where(asset => asset != null));
            }
        }

        // CollectDependencies also returns materials stored inside other assets: a TMP font's material, or one
        // embedded in a model file. It over-reports for prefab variants, whose base prefab's materials count even
        // where the variant overrides them; that only adds a shader or variant the sidecar did not need.
        var variants = new SortedDictionary<string, Material>(StringComparer.Ordinal);
        foreach (Material material in EditorUtility.CollectDependencies(roots.ToArray()).OfType<Material>())
        {
            Shader shader = material.shader;
            if (!NeedsMetalCopy(shader))
            {
                continue;
            }

            string[] keywords = material.shaderKeywords.OrderBy(keyword => keyword, StringComparer.Ordinal).ToArray();
            string key = shader.name + "|" + string.Join(" ", keywords) + "|" + material.enableInstancing;
            if (!variants.ContainsKey(key))
            {
                variants.Add(key, material);
            }
        }

        if (variants.Count == 0)
        {
            return new string[0];
        }

        string folder = CreateCarrierFolder(bundleName.Replace('/', '_'));

        var carrierPaths = new List<string>();
        foreach (Material source in variants.Values)
        {
            var carrier = new Material(source.shader)
            {
                shaderKeywords = source.shaderKeywords,
                enableInstancing = source.enableInstancing,
            };
            string carrierPath = folder + "/" + carrierPaths.Count + ".mat";
            AssetDatabase.CreateAsset(carrier, carrierPath);
            carrierPaths.Add(carrierPath);
        }

        AssetDatabase.SaveAssets();
        return carrierPaths.ToArray();
    }

    // Project shaders and built-in ones from unity_builtin_extra are copied into each bundle that uses them, so
    // the bundle's copy is the one that has no Metal programs.
    static bool NeedsMetalCopy(Shader shader)
    {
        return shader != null
            && !shader.name.StartsWith(MockPrefix, StringComparison.Ordinal)
            && shader.name != "Hidden/InternalErrorShader"
            && AssetDatabase.GetAssetPath(shader) != DefaultResourcesPath;
    }

    static string CreateCarrierFolder(string name)
    {
        if (!AssetDatabase.IsValidFolder(CarrierRoot))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(CarrierRoot), Path.GetFileName(CarrierRoot));
        }
        string folder = CarrierRoot + "/" + name;
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder(CarrierRoot, name);
        }
        return folder;
    }

    static void DeleteCarriers()
    {
        if (AssetDatabase.IsValidFolder(CarrierRoot))
        {
            AssetDatabase.DeleteAsset(CarrierRoot);
        }
    }

    // Unity never removes the output of a bundle that is no longer built, and the mods' build guard insists on
    // embedding any sidecar it finds beside a bundle.
    static void DeleteStaleSidecars(string outputPath, string[] built)
    {
        foreach (string file in Directory.GetFiles(outputPath, "*" + Suffix))
        {
            if (!built.Contains(Path.GetFileName(file)))
            {
                File.Delete(file);
                File.Delete(file + ".manifest");
            }
        }
    }
}
