using SharpCompress.Archives;
using SharpCompress.Readers;
using System.IO;
using System.Linq;
using System.Net;
using ThunderKit.Core.Data;
using ThunderKit.Core.Editor;
using UnityEditor;

namespace ThunderKit.Integrations.Thunderstore
{
    using PV = Core.Data.PackageVersion;
    public class ThunderstoreSource : PackageSource
    {
        internal readonly static string CachePath = $"Assets/ThunderKitSettings/{typeof(ThunderstoreSource).Name}.asset";

        [InitializeOnLoadMethod]
        public static void SetupInitialization()
        {
            InitializeSources -= PackageSource_InitializeSources;
            InitializeSources += PackageSource_InitializeSources;
        }

        private static void PackageSource_InitializeSources(object sender, System.EventArgs e)
        {
            //AssetDatabase.DeleteAsset(CachePath);
            ThunderstoreAPI.PagesLoaded -= ThunderstoreAPI_PagesLoaded;
            ThunderstoreAPI.PagesLoaded += ThunderstoreAPI_PagesLoaded;
            ThunderstoreAPI.ReloadPages();
        }

        private static void ThunderstoreAPI_PagesLoaded(object sender, System.EventArgs e)
        {
            ThunderstoreAPI.PagesLoaded -= ThunderstoreAPI_PagesLoaded;
            var tss = ScriptableHelper.EnsureAsset<ThunderstoreSource>(CachePath, source =>
            {
                source.hideFlags = UnityEngine.HideFlags.NotEditable;
            });
            tss.LoadPackages();
        }

        public override string Name => "Thunderstore";

        public override string SourceGroup => "Thunderstore";
        protected override string VersionIdToGroupId(string dependencyId) => dependencyId.Substring(0, dependencyId.LastIndexOf("-"));

        protected override void OnLoadPackages()
        {
            var loadedPackages = ThunderstoreAPI.PackageListings;
            var realMods = loadedPackages.Where(tsp => !tsp.categories.Contains("Modpacks"));
            //var orderByPinThenName = realMods.OrderByDescending(tsp => tsp.is_pinned).ThenBy(tsp => tsp.name);
            foreach (var tsp in realMods)
            {
                var versions = tsp.versions.Select(v => new PackageVersionInfo(v.version_number, v.full_name, v.dependencies));
                AddPackageGroup(tsp.owner, tsp.name, tsp.Latest.description, tsp.full_name, tsp.categories, versions);
            }

            SourceUpdated();
        }

        protected override void OnInstallPackageFiles(PV version, string packageDirectory)
        {
            var tsPackage = ThunderstoreAPI.LookupPackage(version.group.DependencyId).First();
            var tsPackageVersion = tsPackage.versions.First(tspv => tspv.version_number.Equals(version.version));
            var filePath = Path.Combine(packageDirectory, $"{tsPackageVersion.full_name}.zip");

            using (var client = new WebClient())
            {
                client.DownloadFile(tsPackageVersion.download_url, filePath);
            }

            using (var archive = ArchiveFactory.Open(filePath))
            {
                foreach (var entry in archive.Entries.Where(entry => entry.IsDirectory))
                {
                    var path = Path.Combine(packageDirectory, entry.Key);
                    Directory.CreateDirectory(path);
                }

                var extractOptions = new ExtractionOptions { ExtractFullPath = true, Overwrite = true };
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    entry.WriteToDirectory(packageDirectory, extractOptions);
            }

            File.Delete(filePath);
        }
    }
}