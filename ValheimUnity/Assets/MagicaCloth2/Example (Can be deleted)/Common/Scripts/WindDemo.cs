// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    public class WindDemo : MonoBehaviour
    {
        [SerializeField]
        private MagicaWindZone magicaWindZone;
        [SerializeField]
        private WindZone unityWindZone;
        [SerializeField]
        private Renderer arrowRenderer = null;
        [SerializeField]
        private Gradient arrowGradient = new Gradient();
        [SerializeField]
        private List<Transform> rotationTransforms = new List<Transform>();

        private float angleY = 0.0f;
        private float angleX = 0.0f;
        private float main = 0.0f;
        private float turbulence = 0.0f;

        public void OnDirectionY(float value)
        {
            angleY = value;
            UpdateDirection();
        }

        public void OnDirectionX(float value)
        {
            angleX = value;
            UpdateDirection();
        }

        public void OnMain(float value)
        {
            main = value;
            UpdateMagicaWindZone();
            UpdateUnityWindZone();
            UpdateArrowColor();
        }

        public void OnTurbulence(float value)
        {
            turbulence = value;
            UpdateMagicaWindZone();
        }

        //=========================================================================================
        void UpdateArrowColor()
        {
            if (arrowRenderer)
            {
                // color
                var t = Mathf.Clamp01(Mathf.InverseLerp(0.0f, 20.0f, main));
                var col = arrowGradient.Evaluate(t);
                arrowRenderer.material.color = col * 0.7f;
            }
        }

        void UpdateDirection()
        {
            var lrot = Quaternion.Euler(angleX, angleY, 0.0f);
            foreach (var t in rotationTransforms)
                if (t)
                    t.localRotation = lrot;

            UpdateMagicaWindZone();
        }

        void UpdateMagicaWindZone()
        {
            if (magicaWindZone)
            {
                magicaWindZone.main = main;
                magicaWindZone.turbulence = turbulence;
                magicaWindZone.directionAngleX = angleX;
                magicaWindZone.directionAngleY = angleY;
            }
        }

        void UpdateUnityWindZone()
        {
            if (unityWindZone)
            {
                unityWindZone.windMain = main;
            }
        }
    }
}
