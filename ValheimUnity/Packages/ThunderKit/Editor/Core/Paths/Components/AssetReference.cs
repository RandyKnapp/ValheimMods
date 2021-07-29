using ThunderKit.Core.Manifests;
using ThunderKit.Core.Pipelines;
using UnityEditor;

namespace ThunderKit.Core.Paths.Components
{
    public class AssetReference : PathComponent
    {
        public DefaultAsset Asset;
        public override string GetPath(PathReference output, Pipeline pipeline) => AssetDatabase.GetAssetPath(Asset);
    }
}
