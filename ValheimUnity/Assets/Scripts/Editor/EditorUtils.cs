using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class EditorUtils
{
    [MenuItem("Mod/Build Asset Bundles")]
    static void BuildAssetBundles()
    {
        string stageDirName = "AssetBundles";
        string projectRootDir = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string stagePath = Path.Combine(projectRootDir, stageDirName);

        // Removed before building, so a build that fails or is cancelled leaves no record vouching
        // for whatever bundles are left in the folder.
        BuildRecord.Delete(stagePath);

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(stagePath, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        if (manifest == null)
        {
            Debug.LogError("The asset bundle build failed, so no build record was written.");
            return;
        }

        BuildRecord.Write(projectRootDir, stagePath, manifest.GetAllAssetBundles());
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

// What `mod-tools check` reads to tell whether a bundle that is not committed (epicloot) was built from
// this Unity project as it is now: the commit, the git blob id of every source file that differed from
// it, and each bundle's SHA-256. Written beside the bundles after every build, and gitignored there.
//
// The format and the git commands are mod-tools' contract (its SETUP.md, "Asset bundles"), and its
// check runs the same commands in the same directory to compare. Change them only together with it.
static class BuildRecord
{
    const string FileName = "build-record.json";
    const int Format = 1;

    // The directories the editor reads. Library, Temp and Logs are caches rebuilt from these.
    static readonly string[] Sources = { "Assets", "Packages", "ProjectSettings" };

    // JsonUtility cannot write a dictionary, so each map is a list of two-field objects.
    [Serializable] public class Changed { public string path; public string blob; }
    [Serializable] public class Bundle { public string name; public string sha256; }

    [Serializable]
    public class Record
    {
        public int format;
        public string[] sources;
        public string commit;
        public Changed[] changed;
        public string[] deleted;
        public Bundle[] bundles;
    }

    public static void Delete(string outputDir)
    {
        string path = Path.Combine(outputDir, FileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void Write(string projectDir, string outputDir, string[] bundleNames)
    {
        try
        {
            string[] present = Paths(Git(projectDir, Diff("d")))
                .Union(Paths(Git(projectDir, "ls-files -z --others --exclude-standard -- " + Pathspecs())))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] blobs = HashObjects(projectDir, present);

            var record = new Record
            {
                format = Format,
                sources = Sources,
                commit = Git(projectDir, "rev-parse HEAD").Trim(),
                changed = present.Select((path, i) => new Changed { path = path, blob = blobs[i] }).ToArray(),
                deleted = Paths(Git(projectDir, Diff("D"))),
                bundles = bundleNames
                    .Select(name => new Bundle { name = name, sha256 = Sha256(Path.Combine(outputDir, name)) })
                    .ToArray(),
            };

            string target = Path.Combine(outputDir, FileName);
            string temporary = target + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(record, true), new UTF8Encoding(false));
            Delete(outputDir);
            File.Move(temporary, target);

            int uncommitted = record.changed.Length + record.deleted.Length;
            Debug.Log("Asset bundle build record written: " + record.bundles.Length + " bundles from " +
                record.commit.Substring(0, 7) + (uncommitted > 0 ? ", with " + uncommitted + " uncommitted files" : ""));
        }
        catch (Exception e)
        {
            Delete(outputDir);
            Debug.LogError("The asset bundles were built, but no build record was written, so mod-tools " +
                "will not package a mod that embeds one that is not committed: " + e.Message);
        }
    }

    static string Pathspecs() => string.Join(" ", Sources.Select(source => "\":(literal)" + source + "\""));

    // Lowercase d keeps every change but deletions; uppercase D is only deletions. Relative to this
    // project, which is how the record names files.
    static string Diff(string filter) =>
        "diff --name-only -z --no-renames --relative --no-ext-diff --diff-filter=" + filter + " HEAD -- " + Pathspecs();

    static string[] Paths(string output) => output.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

    // Named on the command line rather than with --stdin-paths, which reads its paths from the
    // repository root instead of this directory. Batched to stay inside Windows' command-line limit.
    static string[] HashObjects(string dir, string[] paths)
    {
        var blobs = new List<string>();
        for (int start = 0; start < paths.Length;)
        {
            var arguments = new StringBuilder("hash-object --");
            int end = start;
            while (end < paths.Length && (end == start || arguments.Length + paths[end].Length < 16000))
            {
                arguments.Append(" \"").Append(paths[end]).Append('"');
                end++;
            }

            blobs.AddRange(Git(dir, arguments.ToString()).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
            start = end;
        }

        if (blobs.Count != paths.Length)
        {
            throw new Exception("git hash-object gave " + blobs.Count + " ids for " + paths.Length + " files");
        }
        return blobs.ToArray();
    }

    static string Sha256(string path)
    {
        using (SHA256 sha = SHA256.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
        }
    }

    // core.longpaths because some .meta files here are past Windows' 260-character limit, and without
    // it git cannot open them and reports them as missing.
    static string Git(string dir, string arguments)
    {
        var info = new ProcessStartInfo("git", "-C \"" + dir + "\" -c core.longpaths=true " + arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using (Process process = Process.Start(info))
        {
            var error = process.StandardError.ReadToEndAsync();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new Exception("git " + arguments + " failed: " + error.Result.Trim());
            }
            return output;
        }
    }
}