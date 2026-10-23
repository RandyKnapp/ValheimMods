// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// CapsuleColliderのエディタ拡張
    /// </summary>
    [CustomEditor(typeof(MagicaCapsuleCollider))]
    [CanEditMultipleObjects]
    public class MagicaCapsuleColliderEditor : MagicaEditorBase
    {
        public override void OnInspectorGUI()
        {
            var scr = target as MagicaCapsuleCollider;
            const string undoName = "CapsuleCollider";

            serializedObject.Update();
            Undo.RecordObject(scr, undoName);

            // separation
            var separationValue = serializedObject.FindProperty("radiusSeparation");

            // direction
            var directionValue = serializedObject.FindProperty("direction");
            EditorGUILayout.PropertyField(directionValue);

            // reverse direction
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reverseDirection"));

            // aligned on center
            var alignedOnCenterValue = serializedObject.FindProperty("alignedOnCenter");
            EditorGUILayout.PropertyField(alignedOnCenterValue);

            var sizeValue = serializedObject.FindProperty("size");
            var size = sizeValue.vector3Value;

            // マルチ選択時について
            // CapsuleColliderではEditorGUI.showMixedValueは設定しない
            // EditorGUI.showMixedValueを設定すると異なる値の場合は「ー」で表記されるようになるが、
            // カプセルコライダーでは１つのVector3に３つの異なるスライダーを割り当てているため、
            // 各スライダーの値が同じ場合でも「ー」表記になってしまう
            // そうならないようにアクティブなコンポーネントの数値のみを表示するように、showMixedValueは常にfalseにしておく

            // length
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newSize = EditorGUILayout.Slider("Length", size.z, 0.0f, 2.0f);
                ApplyMultiSelection<MagicaCapsuleCollider>(check.changed, undoName, x => x.SetSizeZ(newSize));
            }

            // radius
            float lineHight = EditorGUIUtility.singleLineHeight;

            // start
            {
                Rect r = EditorGUILayout.GetControlRect();

                // ラベルを描画
                var positionA = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(scr.radiusSeparation ? "Start Radius" : "Radius"));

                // 矩形を計算
                float w = positionA.width;
                var buttonRect = new Rect(positionA.x + w - 30, r.y, 30, lineHight);
                var sliderRect = new Rect(positionA.x, r.y, Mathf.Max(w - 35, 0), lineHight);

                // Slider
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var newSize = EditorGUI.Slider(sliderRect, size.x, 0.001f, 0.5f);
                    ApplyMultiSelection<MagicaCapsuleCollider>(check.changed, undoName, x => x.SetSizeX(newSize));
                }

                // 分割ボタン
                if (GUI.Button(buttonRect, scr.radiusSeparation ? "X" : "S"))
                {
                    // 切り替え
                    separationValue.boolValue = !separationValue.boolValue;
                }
            }

            // end
            if (separationValue.boolValue)
            {
                Rect r = EditorGUILayout.GetControlRect();

                // ラベルを描画
                var positionA = EditorGUI.PrefixLabel(r, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("End Radius"));

                // 矩形を計算
                float w = positionA.width;
                var sliderRect = new Rect(positionA.x, r.y, Mathf.Max(w - 35, 0), lineHight);

                // Slider
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var newSize = EditorGUI.Slider(sliderRect, size.y, 0.001f, 0.5f);
                    ApplyMultiSelection<MagicaCapsuleCollider>(check.changed, undoName, x => x.SetSizeY(newSize));
                }
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
