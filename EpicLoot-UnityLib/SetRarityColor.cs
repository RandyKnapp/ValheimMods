using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public enum MagicRarityUnity
    {
        None = -1,
        Magic,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    public class SetRarityColor : MonoBehaviour
    {
        public MagicRarityUnity Rarity = MagicRarityUnity.None;
        public Graphic[] Graphics;

        public delegate Color GetRarityColorDelegate(MagicRarityUnity rarity);

        public static GetRarityColorDelegate GetRarityColor;

        private readonly Dictionary<Graphic, Color> _defaultColors = new Dictionary<Graphic, Color>();

        public void Awake()
        {
            foreach (var graphic in Graphics)
            {
                _defaultColors.Add(graphic, graphic.color);
            }

            Refresh();
        }

        public void SetRarity(MagicRarityUnity rarity)
        {
            Rarity = rarity;
            Refresh();
        }

        public void Refresh()
        {
            if (Rarity > MagicRarityUnity.None && GetRarityColor != null)
            {
                var color = GetRarityColor(Rarity);
                foreach (var graphic in Graphics)
                {
                    graphic.color = color;
                }

                SetColor(color);
            }
            else
            {
                foreach (var graphic in Graphics)
                {
                    graphic.color = _defaultColors[graphic];
                }
            }
        }

        // Copy of BeamColorSetter in EpicLoot
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
