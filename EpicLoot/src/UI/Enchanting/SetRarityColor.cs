using System.Collections.Generic;
using EpicLoot;
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
        // rarity is set.
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
                Color color = EpicLoot.EpicLoot.GetRarityColorARGB(Rarity);
                foreach (Graphic graphic in Graphics)
                {
                    graphic.color = color;
                }

                EpicLoot.LootBeams.BeamColor.Apply(gameObject, color);
            }
            else
            {
                foreach (Graphic graphic in Graphics)
                {
                    graphic.color = _defaultColors[graphic];
                }
            }
        }
    }
}
