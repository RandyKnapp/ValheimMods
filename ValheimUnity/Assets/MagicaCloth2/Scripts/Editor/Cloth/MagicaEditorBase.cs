// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp
using System;
using UnityEditor;

namespace MagicaCloth2
{
    /// <summary>
    /// インスペクター拡張のベースクラス
    /// </summary>
    public class MagicaEditorBase : Editor
    {
        /// <summary>
        /// マルチ選択編集の適用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="changed"></param>
        /// <param name="act"></param>
        protected void ApplyMultiSelection<T>(bool changed, string undoName, Action<T> act) where T : ClothBehaviour
        {
            if (changed)
            {
                foreach (var obj in targets)
                {
                    var tscr = obj as T;

                    // Undo
                    Undo.RecordObject(tscr, undoName);

                    act(tscr);
                    EditorUtility.SetDirty(tscr);

                    // OnValidate()手動呼び出し
                    tscr.GetType().GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(tscr, null);
                }
            }
        }
    }
}
