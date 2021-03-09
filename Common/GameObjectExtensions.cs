using UnityEngine;

namespace Common
{
    public static class GameObjectExtensions
    {
        public static RectTransform RectTransform(this GameObject go)
        {
            return go.transform as RectTransform;
        }
    }
}
