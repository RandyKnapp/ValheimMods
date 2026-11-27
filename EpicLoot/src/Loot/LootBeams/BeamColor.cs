using UnityEngine;

namespace EpicLoot.LootBeams
{
    internal static class BeamColor
    {
        // Recolours a beam or a crafting material's glow to a rarity's hue, keeping each part's own brightness.
        // The line renderers' tint is set on their shared material, so every copy of that prefab takes it.
        public static void Apply(GameObject root, Color color)
        {
            foreach (LineRenderer lineRenderer in root.GetComponentsInChildren<LineRenderer>(true))
            {
                foreach (Material mat in lineRenderer.sharedMaterials)
                {
                    mat.SetColor("_TintColor", KeepLuminosity(color, mat.GetColor("_TintColor")));
                }
            }

            foreach (ParticleSystem particleSystem in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particleSystem.main;
                switch (main.startColor.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        main.startColor = new ParticleSystem.MinMaxGradient(KeepLuminosity(color, main.startColor.color));
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        main.startColor = new ParticleSystem.MinMaxGradient(
                            KeepLuminosity(color, main.startColor.colorMin),
                            KeepLuminosity(color, main.startColor.colorMax));
                        break;
                }
                particleSystem.Clear();
                particleSystem.Play();
            }
        }

        private static Color KeepLuminosity(Color newColor, Color baseColor)
        {
            Color.RGBToHSV(newColor, out float h, out float s, out _);
            Color.RGBToHSV(baseColor, out _, out _, out float v);
            return Color.HSVToRGB(h, s, v);
        }
    }
}
