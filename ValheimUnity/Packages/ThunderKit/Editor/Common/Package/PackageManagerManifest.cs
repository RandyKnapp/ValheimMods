using System;
using System.Collections.Generic;

namespace ThunderKit.Common.Package
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
}