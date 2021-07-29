using System.Diagnostics;
using System.Text;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Paths;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline))]
    public class ExecuteProcess : PipelineJob
    {
        [PathReferenceResolver]
        public string workingDirectory;
        [PathReferenceResolver]
        public string executable;
        [PathReferenceResolver]
        public string[] arguments;

        public override void Execute(Pipeline pipeline)
        {
            var args = new StringBuilder();
            for (int i = 0; i < arguments.Length; i++)
            {
                args.Append(arguments[i].Resolve(pipeline, this));
                args.Append(" ");
            }

            var exe = executable.Resolve(pipeline, this);
            var pwd = workingDirectory.Resolve(pipeline, this);
            var rorPsi = new ProcessStartInfo(exe)
            {
                WorkingDirectory = pwd,
                Arguments = args.ToString(),
                //Standard output redirection doesn't currently work with bepinex, appears to be considered a bepinex bug
                //RedirectStandardOutput = true,
                UseShellExecute = true
            };

            Process.Start(rorPsi);
        }
    }
}
