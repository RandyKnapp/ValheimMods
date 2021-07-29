using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThunderKit.Core.Data;
using UnityEditor;

namespace ThunderKit.Core.Editor
{
    public static class Extensions
    {
        public static IEnumerable<IncludedSettings> GetFlags(this IncludedSettings input)
        {
            foreach (IncludedSettings value in (IncludedSettings[])Enum.GetValues(typeof(IncludedSettings)))
                if ((input & value) == value)
                    yield return value;
        }
        public static IEnumerable<FileAttributes> GetFlags(this FileAttributes input)
        {
            foreach (FileAttributes value in (FileAttributes[])Enum.GetValues(typeof(FileAttributes)))
                if ((input & value) == value)
                    yield return value;
        }

        public static bool HasFlag(this FileAttributes input, FileAttributes flag)
        {
            return (input & flag) == flag;
        }
        public static bool HasFlag(this ExportPackageOptions input, ExportPackageOptions flag)
        {
            return (input & flag) == flag;
        }

    }
}