using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace ThunderKit.Core
{
    public abstract class ComposableObject : ScriptableObject
    {
        [FormerlySerializedAs("runSteps")]
        public ComposableElement[] Data;

        public abstract bool SupportsType(Type type);

        public abstract Type ElementType { get; }

        public abstract string ElementTemplate { get; }

        public void InsertElement(ComposableElement instance, int index)
        {
            if (!ElementType.IsAssignableFrom(instance.GetType())) return;
            if (!SupportsType(instance.GetType())) return;

            AssetDatabase.AddObjectToAsset(instance, this);

            var so = new SerializedObject(this);
            var dataArray = so.FindProperty(nameof(Data));

            dataArray.InsertArrayElementAtIndex(index);
            var stepField = dataArray.GetArrayElementAtIndex(index);
            stepField.objectReferenceValue = instance;
            stepField.serializedObject.SetIsDifferentCacheDirty();
            stepField.serializedObject.ApplyModifiedProperties();

            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(instance));
        }

        public void RemoveElement(ComposableElement instance, int index)
        {
            var so = new SerializedObject(this);
            var dataArray = so.FindProperty(nameof(Data));
            var elementAtIndex = dataArray.GetArrayElementAtIndex(index).objectReferenceValue as ComposableElement;
            if (elementAtIndex != instance)
            {
                Debug.LogError("ComposableObject.RemoveElement: instance does not match index");
                return;
            }
            AssetDatabase.RemoveObjectFromAsset(instance);

            dataArray.DeleteArrayElementAtIndex(index);

            DestroyImmediate(instance);

            for (int x = index; x < dataArray.arraySize; x++)
                dataArray.MoveArrayElement(x + 1, x);

            dataArray.arraySize--;

            so.SetIsDifferentCacheDirty();
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}