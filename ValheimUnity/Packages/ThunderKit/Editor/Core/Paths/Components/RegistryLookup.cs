using ThunderKit.Core.Pipelines;
using ThunderKit.Core.Paths;
using Microsoft.Win32;

namespace ThunderKit.Core.Paths.Components
{
    public class RegistryLookup : PathComponent
    {
        public string KeyName;
        public string ValueName;

        public override string GetPath(PathReference output, Pipeline pipeline)
        {
            return (string)Registry.GetValue(KeyName, ValueName, string.Empty);
        }
    }
}
