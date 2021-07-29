using System;
using System.Linq;

namespace ThunderKit.Integrations.Thunderstore
{
    [Serializable]
    public partial class PackageListing
    {
        public string name;
        public string full_name;
        public string owner;
        public string package_url;
        public DateTime date_created;
        public DateTime date_updated;
        public long rating_score;
        public bool is_pinned;
        public bool is_deprecated;
        public string total_downloads;
        public string uuid4;
        public PackageVersion[] versions;
        public bool has_nsfw_content;
        public string[] categories;
        public PackageVersion Latest => versions.First();
    }
}