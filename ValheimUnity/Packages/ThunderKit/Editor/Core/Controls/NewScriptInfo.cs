using System;
using UnityEngine;

namespace ThunderKit.Core.Editor.Controls
{
    [Serializable]
    class NewScriptInfo : ScriptableObject
    {
        public string scriptPath;
        public bool addAsset;

        public static NewScriptInfo Instance
        {
            get
            {
                var objs = Resources.FindObjectsOfTypeAll<NewScriptInfo>();
                if (objs.Length == 0 || objs[0] == null)
                {
                    return ScriptableObject.CreateInstance<NewScriptInfo>();
                }
                return objs[0];
            }
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        public void Reset()
        {
            addAsset = false;
            scriptPath = string.Empty;
        }
    }
}