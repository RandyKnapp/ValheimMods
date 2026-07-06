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
            foreach (Graphic graphic in Graphics)
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
                Color color = GetRarityColor(Rarity);
                foreach (Graphic graphic in Graphics)
                {
                    graphic.color = color;
                }

                SetColor(color);
            }
            else
            {
                foreach (Graphic graphic in Graphics)
                {
                    graphic.color = _defaultColors[graphic];
                }
            }
        }

        // Copy of BeamColorSetter in EpicLoot
        public void SetColor(Color mid)
        {
            LineRenderer[] allBeams = GetComponentsInChildren<LineRenderer>();
            ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();

            foreach (LineRenderer lineRenderer in allBeams)
            {
                foreach (Material mat in lineRenderer.sharedMaterials)
                {
                    mat.SetColor("_TintColor", SwapColorKeepLuminosity(mid, mat.GetColor("_TintColor")));
                }
            }

            foreach (ParticleSystem particleSystem in allParticles)
            {
                ParticleSystem.MainModule main = particleSystem.main;
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
