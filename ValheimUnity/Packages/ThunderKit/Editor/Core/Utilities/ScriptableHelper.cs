using System;
using System.IO;
using ThunderKit.Core.Editor.Actions;
using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Editor
{
    public static class ScriptableHelper
    {
        readonly static object[] findTextureParams = new object[1];
        public static void SelectNewAsset<T>(Func<string> overrideName = null, Action<T> afterCreated = null) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            var name = typeof(T).Name;
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");
            Action<int, string, string> action =
                (int instanceId, string pathname, string resourceFile) =>
                  {
                      AssetDatabase.CreateAsset(asset, pathname);
                      AssetDatabase.SaveAssets();
                      AssetDatabase.Refresh();
                      Selection.activeObject = asset;
                      afterCreated?.Invoke(asset);
                  };
            if (overrideName == null)
            {
                var endAction = ScriptableObject.CreateInstance<SelfDestructingActionAsset>();
                endAction.action = action;
                var findTexture = typeof(EditorGUIUtility).GetMethod(nameof(EditorGUIUtility.FindTexture), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                findTextureParams[0] = typeof(T);
                var icon = (Texture2D)findTexture.Invoke(null, findTextureParams);
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(asset.GetInstanceID(), endAction, assetPathAndName, icon, null);
            }
            else
            {
                name = overrideName();
                assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");

                action(asset.GetInstanceID(), assetPathAndName, null);
            }
        }

        public static void SelectNewAsset(Type t, Func<string> overrideName = null)
        {
            if (!typeof(ScriptableObject).IsAssignableFrom(t)) return;

            var asset = ScriptableObject.CreateInstance(t);

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            var name = overrideName == null ? t.Name : overrideName();
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static T EnsureAsset<T>(string assetPath, Action<T> initializer = null) where T : ScriptableObject
        {
            var settings = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<T>();
                initializer?.Invoke(settings);
                AssetDatabase.CreateAsset(settings, assetPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }
}