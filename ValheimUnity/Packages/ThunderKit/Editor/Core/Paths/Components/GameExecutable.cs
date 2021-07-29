using ThunderKit.Core.Data;
using ThunderKit.Core.Manifests;
using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class GameExecutable : PathComponent
    {
        public override string GetPath(PathReference output, Pipeline pipeline) => ThunderKitSettings.GetOrCreateSettings<ThunderKitSettings>().GameExecutable;
    }
}
