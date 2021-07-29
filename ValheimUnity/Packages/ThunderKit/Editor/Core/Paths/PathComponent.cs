using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths
{
    public class PathComponent : ComposableElement
    {
        public virtual string GetPath(PathReference output, Pipeline pipeline) => "";
    }
}