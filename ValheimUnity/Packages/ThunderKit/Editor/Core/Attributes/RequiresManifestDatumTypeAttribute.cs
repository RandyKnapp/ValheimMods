using System;
using System.Linq;
using ThunderKit.Core.Manifests;

namespace ThunderKit.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RequiresManifestDatumTypeAttribute : Attribute
    {
        readonly Type[] datumTypes;
        public RequiresManifestDatumTypeAttribute(params Type[] datumTypes)
        {
            this.datumTypes = datumTypes;
        }

        public bool CanProcessManifest(Manifest manifest)
        {
            var allDatumTypes = manifest.Data.Select(d => d.GetType()).ToArray();
            return datumTypes.All(type => allDatumTypes.Any(type.IsAssignableFrom));
        }
    }
}