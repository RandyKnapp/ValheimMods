using System.IO;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Editor;
using System;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline))]
    public class Delete : FlowPipelineJob
    {
        public bool Recursive;
        public bool IsFatal;
        public string Path;

        protected override void ExecuteInternal(Pipeline pipeline)
        {
            var path = Path.Resolve(pipeline, this);
            var pathIsFile = false;
            try
            {
                pathIsFile = !File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            }
            catch (Exception e)
            {
                if (IsFatal) 
                    throw e;
            }

            try
            {
                if (pathIsFile) File.Delete(path);
                else
                    Directory.Delete(path, Recursive);
            }
            catch (Exception e)
            {
                if (IsFatal)
                    throw e;
            }
        }
    }
}
