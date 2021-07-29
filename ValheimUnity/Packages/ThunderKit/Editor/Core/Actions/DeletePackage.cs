using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Editor.Actions
{
    public class DeletePackage : ScriptableObject
    {
        public string directory;

        public bool TryDelete()
        {
            try
            {
                return !EditorApplication.isCompiling && FileUtil.DeleteFileOrDirectory(directory);
            }
            catch { }
            return !Directory.Exists(directory);
        }
    }
}