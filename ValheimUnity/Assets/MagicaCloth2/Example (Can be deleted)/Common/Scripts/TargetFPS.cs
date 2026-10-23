// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    public class TargetFPS : MonoBehaviour
    {
        public int frameRate = 60;

#if !UNITY_EDITOR
        protected void Start()
        {
            Application.targetFrameRate = frameRate;
        }
#endif
    }
}
