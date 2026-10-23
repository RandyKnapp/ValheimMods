using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Keeps the HUD health bar on screen at high max health. Vanilla grows it by the same length for every
    /// point of health with no limit, so gear adding hundreds of health pushed it far up the screen. Up to
    /// <see cref="FullGrowthHealth"/> it grows as in vanilla; past that each point adds less, until at
    /// <see cref="MaxLengthHealth"/> it is <see cref="MaxScreenFraction"/> of the screen's height and stops.
    /// </summary>
    /// <remarks>
    /// The bar is rotated a quarter turn in the HUD prefab, so its horizontal size is its height on screen.
    /// The food bar beside it is sized from the same max health in <c>Hud.UpdateFood</c> and is shrunk by
    /// the same ratio, so the two stay level. The fills are fractions of the bar (<c>GuiBar.SetMaxValue</c>),
    /// so they need nothing.
    /// </remarks>
    [HarmonyPatch]
    internal static class HealthBarSize_Patch
    {
        // Vanilla's bar length per point of max health (Hud.UpdateHealth: maxHealth / 25 * 32).
        private const float LengthPerHealth = 32f / 25f;
        private const float FullGrowthHealth = 100f;
        private const float MaxLengthHealth = 1000f;
        private const float MaxScreenFraction = 0.5f;

        private static Hud _hud;
        private static RectTransform _canvas;

        [HarmonyPatch(typeof(Hud), nameof(Hud.SetHealthBarSize))]
        [HarmonyPrefix]
        private static void SetHealthBarSize_Prefix(Hud __instance, ref float size)
        {
            size = Compress(__instance, size);
        }

        // UpdateFood runs before UpdateHealth each frame, so this sets the food bar to the length the health
        // bar is about to get.
        [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateFood))]
        [HarmonyPostfix]
        private static void UpdateFood_Postfix(Hud __instance, Player player)
        {
            float length = player.GetMaxHealth() * LengthPerHealth;
            float compressed = Compress(__instance, length);
            if (compressed >= length)
            {
                return;
            }

            float ratio = compressed / length;
            __instance.m_foodBarRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Ceil(compressed));
            __instance.m_foodBaseBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                player.GetBaseFoodHP() * LengthPerHealth * ratio);
        }

        private static float Compress(Hud hud, float length)
        {
            float knee = FullGrowthHealth * LengthPerHealth;
            // Auga draws its own health bar.
            if (length <= knee || EpicLoot.HasAuga || !TryGetMaxLength(hud, out float max))
            {
                return length;
            }

            // Never shorter than vanilla's bar at the knee, however small the screen or large the GUI scale.
            max = Mathf.Max(max, knee);
            float full = MaxLengthHealth * LengthPerHealth;
            float t = Mathf.Clamp01((length - knee) / (full - knee));
            // Ease-out: growth slows from the knee and reaches zero exactly at full. The Min keeps the bar
            // from ever growing faster than vanilla, which a tall canvas would otherwise allow just past the knee.
            float eased = 1f - (1f - t) * (1f - t);
            return Mathf.Min(length, knee + (max - knee) * eased);
        }

        // The longest the bar may be, in its own units: the HUD canvas is resized by the GUI scale
        // (GuiScaler), so the screen's height is read from the canvas rather than from Screen.height.
        private static bool TryGetMaxLength(Hud hud, out float max)
        {
            max = 0f;
            RectTransform bar = hud.m_healthBarRoot;
            if (bar == null)
            {
                return false;
            }

            if (_hud != hud || _canvas == null)
            {
                _hud = hud;
                Canvas canvas = bar.GetComponentInParent<Canvas>();
                _canvas = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
            }

            if (_canvas == null)
            {
                return false;
            }

            // Canvas units per bar unit: 1 in the vanilla prefab, but another mod may scale the HUD.
            float canvasScale = Mathf.Abs(_canvas.lossyScale.y);
            float barScale = Mathf.Abs(bar.lossyScale.x);
            float height = _canvas.rect.height;
            if (canvasScale <= 0f || barScale <= 0f || height <= 0f)
            {
                return false;
            }

            max = MaxScreenFraction * height * canvasScale / barScale;
            return true;
        }
    }
}
