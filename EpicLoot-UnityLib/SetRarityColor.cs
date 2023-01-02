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
            }
            else
            {
                foreach (var graphic in Graphics)
                {
                    graphic.color = _defaultColors[graphic];
                }
            }
        }
    }

}
