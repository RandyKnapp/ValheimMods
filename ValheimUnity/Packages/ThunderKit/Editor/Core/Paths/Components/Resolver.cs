using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class Resolver : PathComponent
    {
        public string value;
        public override string GetPath(PathReference output, Pipeline pipeline) => value.Resolve(pipeline, this);
    }
}
