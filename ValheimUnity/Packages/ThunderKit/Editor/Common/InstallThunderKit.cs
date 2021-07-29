using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

using UnityEngine;

namespace ThunderKit.Installer
{
    [Serializable]
    public struct Author
    {
        public string name;
        public string email;
        public string url;
    }
    [Serializable]
    public struct PackageManagerManifest
    {
        public string name;
        public Author author;
        public string displayName;
        public string version;
        public string unity;
        public string description;
        public Dictionary<string, string> dependencies;

        public PackageManagerManifest(Author author, string name, string displayName, string version, string unity, string description)
        {
            this.author = author;
            this.name = name;
            this.displayName = displayName;
            this.version = version;
            this.unity = unity;
            this.description = description;
            this.dependencies = new Dictionary<string, string>();
        }
    }

    public class InstallThunderKit
    {
        [InitializeOnLoadMethod]
        static void IdentifyThunderKit()
        {
            AddScriptingDefine("thunderkit");
        }

        static bool IsObsolete(BuildTargetGroup group)
        {
            var attrs = typeof(BuildTargetGroup).GetField(group.ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false);
            return attrs.Length > 0;
        }

        public static void GeneratePackageManifest(string packageName, string outputDir, string modName, string authorAlias, string modVersion, string description = null, string url = null)
        {
            string unityVersion = Application.unityVersion.Substring(0, Application.unityVersion.LastIndexOf("."));
            var author = new Author
            {
                name = authorAlias,
                url = url
            };
            var packageManifest = new PackageManagerManifest(author, packageName, ObjectNames.NicifyVariableName(modName), modVersion, unityVersion, description);
            var packageManifestJson = JsonUtility.ToJson(packageManifest);
            AddScriptingDefine(packageName);
            File.WriteAllText(Path.Combine(outputDir, "package.json"), packageManifestJson);
        }
        internal static bool ContainsDefine(string define)
        {
            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup))
                    continue;

                string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

                if (!defineSymbols.Contains(define))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Add a define to the scripting define symbols for every build target.
        /// </summary>
        /// <param name="define"></param>
        public static void AddScriptingDefine(string define)
        {
            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup))
                    continue;

                string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

                if (!defineSymbols.Contains(define))
                {
                    if (defineSymbols.Length < 1)
                        defineSymbols = define;
                    else if (defineSymbols.EndsWith(";"))
                        defineSymbols = string.Format("{0}{1}", defineSymbols, define);
                    else
                        defineSymbols = string.Format("{0};{1}", defineSymbols, define);

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
                }
            }
        }

        /// <summary>
        /// Remove a define from the scripting define symbols for every build target.
        /// </summary>
        /// <param name="define"></param>
        public static void RemoveScriptingDefine(string define)
        {
            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (targetGroup == BuildTargetGroup.Unknown || IsObsolete(targetGroup))
                    continue;

                string defineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);

                if (defineSymbols.Contains(define))
                {
                    defineSymbols = defineSymbols.Replace(string.Format("{0};", define), "");
                    defineSymbols = defineSymbols.Replace(define, "");

                    PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, defineSymbols);
                }
            }
        }
    }
}