using System.IO;
using UnityEngine;

namespace ThunderKit.Integrations.Thunderstore
{
    public class CreateThunderstoreManifest
    {
#pragma warning disable 0649
        public struct ThunderstoreManifestStub
        {
            public string name;
            public string author;
            public string version_number;
            public string website_url;
            public string description;
            public string[] dependencies;
        }
#pragma warning restore 0649

        public static ThunderstoreManifestStub LoadStub(string path) => JsonUtility.FromJson<ThunderstoreManifestStub>(File.ReadAllText(path));

    }
}