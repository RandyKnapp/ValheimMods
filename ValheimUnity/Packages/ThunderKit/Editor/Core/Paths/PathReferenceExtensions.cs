using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths
{
    public static class PathReferenceExtensions
    {
        public static string Resolve(this string input, Pipeline pipeline, UnityEngine.Object caller) => PathReference.ResolvePath(input, pipeline, caller);
    }
}