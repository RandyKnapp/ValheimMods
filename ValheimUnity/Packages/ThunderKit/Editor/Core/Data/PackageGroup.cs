using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ThunderKit.Common.Package;
using UnityEngine;

namespace ThunderKit.Core.Data
{
    [Serializable]
    public class PackageGroup : ScriptableObject, IEquatable<PackageGroup>
    {
        public PackageVersion this[string version]
        {
            get
            {
                switch (version)
                {
                    case "latest": return Versions.FirstOrDefault();

                    default: return Versions.FirstOrDefault(pv => pv.version.Equals(version));
                }
            }
        }

        public string PackageName;
        public string Author;
        public string Description;
        [HideInInspector]
        public string DependencyId;
        public string[] Tags;
        public PackageSource Source;
        public PackageVersion[] Versions;
        public string InstallDirectory => Path.Combine("Packages", PackageName);
        public string PackageDirectory => Path.Combine("Packages", DependencyId.ToLower());
        public bool HasString(string value)
        {
            var compareInfo = CultureInfo.InvariantCulture.CompareInfo;

            var authorContains = compareInfo.IndexOf(Author, value, CompareOptions.OrdinalIgnoreCase) > -1;
            if (authorContains) return true;
            var nameContains = compareInfo.IndexOf(PackageName, value, CompareOptions.OrdinalIgnoreCase) > -1;
            if (nameContains) return true;
            var descriptionContains = compareInfo.IndexOf(Description, value, CompareOptions.OrdinalIgnoreCase) > -1;
            if (descriptionContains) return true;
            var dependencyIdContains = compareInfo.IndexOf(DependencyId, value, CompareOptions.OrdinalIgnoreCase) > -1;
            if (dependencyIdContains) return true;
            foreach (var tag in Tags)
            {
                var tagContains = compareInfo.IndexOf(tag, value, CompareOptions.OrdinalIgnoreCase) > -1;
                if (tagContains) return true;
            }

            return false;
        }

        public string InstalledVersion
        {
            get
            {
                if (!File.Exists(Path.Combine(InstallDirectory, "package.json"))) return null;

                var pmm = PackageHelper.GetPackageManagerManifest(InstallDirectory);
                return pmm.version;
            }
        }

        public bool Installed
        {
            get
            {
                if (!File.Exists(Path.Combine(InstallDirectory, "package.json"))) return false;

                var pmm = PackageHelper.GetPackageManagerManifest(InstallDirectory);
                return pmm.name.Equals(DependencyId, StringComparison.OrdinalIgnoreCase);
            }
        }


        public override bool Equals(object obj)
        {
            return Equals(obj as PackageGroup);
        }

        public bool Equals(PackageGroup other)
        {
            return other != null &&
                   DependencyId == other.DependencyId;
        }

        public override int GetHashCode()
        {
            return 996503521 + EqualityComparer<string>.Default.GetHashCode(DependencyId);
        }

        public static bool operator ==(PackageGroup left, PackageGroup right)
        {
            return EqualityComparer<PackageGroup>.Default.Equals(left, right);
        }

        public static bool operator !=(PackageGroup left, PackageGroup right)
        {
            return !(left == right);
        }
    }

}