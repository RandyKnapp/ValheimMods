using ThunderKit.Core.Attributes;

namespace ThunderKit.Core.Manifests
{
    [HideFromScriptWindow]
    public class ManifestDatum : ComposableElement
    {
        [PathReferenceResolver]
        public string[] StagingPaths;
    }
}