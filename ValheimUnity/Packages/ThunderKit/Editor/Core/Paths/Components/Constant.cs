using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class Constant : PathComponent
    {
        public string Value;
        public override string GetPath(PathReference output, Pipeline pipeline) => Value;
    }
}
