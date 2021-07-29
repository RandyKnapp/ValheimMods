using ThunderKit.Core;
using UnityEditor;

namespace Assets.ThunderKit.Core.Editor.Inspectors
{
    [CustomEditor(typeof(ComposableElement), true)]
    public class ComposableElementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
        }
    }
}