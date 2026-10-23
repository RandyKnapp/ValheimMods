// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// ヒエラルキーへアイコンの表示
    /// </summary>
    [InitializeOnLoad]
    public class DrawIconInHierarchy
    {
        const int iconSize = 16;

        static DrawIconInHierarchy()
        {
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawIcon;
#else
            EditorApplication.hierarchyWindowItemOnGUI += DrawIcon;
#endif
        }

        //static void DrawIcon(int instanceId, Rect rect)
#if UNITY_6000_4_OR_NEWER
        static void DrawIcon(EntityId instanceId, Rect rect)
#else
        static void DrawIcon(int instanceId, Rect rect)
#endif
        {
            rect.width = iconSize;
#if UNITY_6000_3_OR_NEWER
            GameObject obj = UnityEditor.EditorUtility.EntityIdToObject(instanceId) as GameObject;
#else
            GameObject obj = UnityEditor.EditorUtility.InstanceIDToObject(instanceId) as GameObject;
#endif
            if (obj == null)
                return;
            rect.x += EditorStyles.label.CalcSize(obj.name).x;
            rect.y += -1;
            rect.x += iconSize + 4;

            foreach (var component in obj.GetComponents<ClothBehaviour>())
            {
                if (component is MagicaSphereCollider
                    || component is MagicaCapsuleCollider
                    || component is MagicaPlaneCollider
                    || component is MagicaCloth
                    || component is MagicaWindZone
                    || component is MagicaSettings
                    )
                {
                    var icon = AssetPreview.GetMiniThumbnail(component);
                    GUI.Label(rect, icon);
                    rect.x += iconSize;
                }
            }
        }
    }

    /// <summary>
    /// テキストのサイズを取得
    /// </summary>
    public static class GUIStyleExtensions
    {
        public static Vector2 CalcSize(this GUIStyle self, string text)
        {
            var content = new GUIContent(text);
            var size = self.CalcSize(content);
            return size;
        }
    }
}
