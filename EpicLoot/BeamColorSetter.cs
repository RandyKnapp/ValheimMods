using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public class BeamColorSetter : MonoBehaviour
    {
        public void SetColor(Color mid)
        {
            var allBeams = GetComponentsInChildren<LineRenderer>();
            var allParticles = GetComponentsInChildren<ParticleSystem>();

            foreach (var lineRenderer in allBeams)
            {
                foreach (var mat in lineRenderer.sharedMaterials)
                {
                    mat.SetColor("_TintColor", SwapColorKeepLuminosity(mid, mat.GetColor("_TintColor")));
                }
            }

            foreach (var particleSystem in allParticles)
            {
                var main = particleSystem.main;
                switch (main.startColor.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        main.startColor = new ParticleSystem.MinMaxGradient(SwapColorKeepLuminosity(mid, main.startColor.color));
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            SwapColorKeepLuminosity(mid, main.startColor.colorMin),
                            SwapColorKeepLuminosity(mid, main.startColor.colorMax));
                        break;
                }
                particleSystem.Clear();
                particleSystem.Play();
            }
        }

        private Color SwapColorKeepLuminosity(Color newColor, Color baseColor)
        {
            Color.RGBToHSV(newColor, out float h, out float s, out float v);
            Color.RGBToHSV(baseColor, out float bh, out float bs, out float bv);
            return Color.HSVToRGB(h, s, bv);
        }
    }
}