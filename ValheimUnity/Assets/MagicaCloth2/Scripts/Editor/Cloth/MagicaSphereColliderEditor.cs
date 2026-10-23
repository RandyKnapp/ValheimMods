// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// SphereColliderのインスペクター拡張
    /// </summary>
    [CustomEditor(typeof(MagicaSphereCollider))]
    [CanEditMultipleObjects]
    public class MagicaSphereColliderEditor : MagicaEditorBase
    {
        public override void OnInspectorGUI()
        {
            var scr = target as MagicaSphereCollider;
            const string undoName = "SphereCollider";

            serializedObject.Update();
            Undo.RecordObject(scr, undoName);

            using (var check = new EditorGUI.ChangeCheckScope())
            {

                // radius
                var sizeValue = serializedObject.FindProperty("size");

                //EditorGUI.showMixedValue = sizeValue.hasMultipleDifferentValues;
                var size = sizeValue.vector3Value;
                float newSize = EditorGUILayout.Slider("Radius", size.x, 0.001f, 0.5f);
                //EditorGUI.showMixedValue = false;

                ApplyMultiSelection<MagicaSphereCollider>(check.changed, undoName, x => x.SetSize(newSize));
            }

            // center
            EditorGUILayout.PropertyField(serializedObject.FindProperty("center"));

            // Symmetry
            EditorGUILayout.Space();
            var symmetryModeProperty = serializedObject.FindProperty("symmetryMode");
            EditorGUILayout.PropertyField(symmetryModeProperty);
            if (symmetryModeProperty.enumValueIndex >= (int)ColliderSymmetryMode.AutomaticTarget)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("symmetryTarget"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
