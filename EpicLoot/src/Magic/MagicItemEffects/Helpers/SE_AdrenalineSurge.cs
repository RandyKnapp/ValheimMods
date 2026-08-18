using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Status effect for Adrenaline Surge, which provides a bonus to health regeneration.
    public class SE_AdrenalineSurge : StatusEffect {
        public float RegenBonus;

        public override void ModifyHealthRegen(ref float regenMultiplier) => regenMultiplier += RegenBonus;

        // Returned with the label left as a token (the callers localize), matching SE_QueenEverflow.
        public override string GetTooltipString() {
            return $"$se_healthregen: <color=orange>+{Mathf.RoundToInt(RegenBonus * 100f)}%</color>";
        }
    }
}
