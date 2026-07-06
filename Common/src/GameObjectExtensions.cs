using UnityEngine;

namespace Common
{
    public static class GameObjectExtensions
    {
        public static RectTransform RectTransform(this GameObject go)
        {
            return go.transform as RectTransform;
        }

        public static T RequireComponent<T>(this GameObject go) where T:Component
        {
            var c = go.GetComponent<T>();
            return c == null ? go.AddComponent<T>() : c;
        }
    }
}
