using System.Collections.Generic;
using EpicLoot;
using EpicLoot.CraftingV2;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SetRarityColor : MonoBehaviour
    {
        public ItemRarity Rarity = (ItemRarity)(-1);
        public Graphic[] Graphics;

        // Every woken instance, so a rarity colour setting change can recolour them. The enchanting table UI is
        // built once per session, and each instance otherwise reads the colours only when it wakes or its
        // rarity is set -- the buttons kept their old colours while the text around them took the new ones.
        private static readonly List<SetRarityColor> _instances = new List<SetRarityColor>();

        private readonly Dictionary<Graphic, Color> _defaultColors = new Dictionary<Graphic, Color>();
        private bool _awake;

        public void Awake()
        {
            foreach (Graphic graphic in Graphics)
            {
                _defaultColors.Add(graphic, graphic.color);
            }

            _awake = true;
            _instances.Add(this);
            Refresh();
        }

        public void OnDestroy()
        {
            _instances.Remove(this);
        }

        public static void RefreshAll()
        {
            foreach (SetRarityColor instance in _instances)
            {
                instance.Refresh();
            }
        }

        /// <summary>Swaps one of the coloured graphics for another (the Auga fixup replaces the label), keeping its default colour.</summary>
        public void ReplaceGraphic(Graphic oldGraphic, Graphic newGraphic)
        {
            int index = oldGraphic != null ? System.Array.IndexOf(Graphics, oldGraphic) : -1;
            if (index < 0)
            {
                index = Graphics.Length;
                System.Array.Resize(ref Graphics, index + 1);
            }

            Graphics[index] = newGraphic;

            // Before Awake (the tab has not been shown yet) Awake itself records the default colours.
            if (!_awake)
            {
                return;
            }

            Color defaultColor = oldGraphic != null && _defaultColors.TryGetValue(oldGraphic, out Color color) ? color : newGraphic.color;
            if (oldGraphic != null)
            {
                _defaultColors.Remove(oldGraphic);
            }

            _defaultColors[newGraphic] = defaultColor;
            Refresh();
        }

        public void SetRarity(ItemRarity rarity)
        {
            Rarity = rarity;
            Refresh();
        }

        public void Refresh()
        {
            if ((int)Rarity >= 0)
            {
                Color color = EnchantingUIController.GetRarityColor(Rarity);
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
