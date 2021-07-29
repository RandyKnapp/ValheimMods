using System;
namespace ThunderKit.Integrations.Thunderstore
{

    [Serializable]
    public partial class PackageVersion
    {
        public string name;
        public string full_name;
        public string description;
        public string icon;
        public string version_number;
        public string[] dependencies;
        public string download_url;
        public int downloads;
        public DateTime date_created;
        public string website_url;
        public bool is_active;
    }
}