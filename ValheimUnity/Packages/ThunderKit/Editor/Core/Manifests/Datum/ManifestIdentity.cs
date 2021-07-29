using ThunderKit.Core.Attributes;
using UnityEngine;

namespace ThunderKit.Core.Manifests.Datum
{
    [HideFromScriptWindow]
    public class ManifestIdentity : ManifestDatum
    {
        public string Author;
        public string Name;
        public Texture2D Icon;
        public string Description;
        public string Version;
        public Manifest[] Dependencies;
    }
}
