using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Editor
{
    public static class EditorHelpers
    {
        public static void AddField(SerializedProperty property, string label = null)
        {
            EditorGUI.BeginChangeCheck();
            if (string.IsNullOrEmpty(label))
                EditorGUILayout.PropertyField(property, true);
            else
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.SetIsDifferentCacheDirty();
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        public static void AddField(Rect rect, SerializedProperty property, string label = null)
        {
            EditorGUI.BeginChangeCheck();
            if (string.IsNullOrEmpty(label))
                EditorGUI.PropertyField(rect, property, true);
            else
                EditorGUI.PropertyField(rect, property, new GUIContent(label), true);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.SetIsDifferentCacheDirty();
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}