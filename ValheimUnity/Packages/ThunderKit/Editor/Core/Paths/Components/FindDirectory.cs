using System.IO;
using System.Linq;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class FindDirectory : PathComponent
    {
        public SearchOption searchOption;
        public string searchPattern;
        [PathReferenceResolver]
        public string path;
        public override string GetPath(PathReference output, Pipeline pipeline)
        {
            string resolvedPath = PathReference.ResolvePath(path, pipeline, this);

            string first = Directory.GetDirectories(resolvedPath, searchPattern, searchOption).First();
            return first;
        }
    }
}
