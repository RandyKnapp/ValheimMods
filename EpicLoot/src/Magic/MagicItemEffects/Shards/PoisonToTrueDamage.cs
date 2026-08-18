using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Converts a portion of outgoing poison damage into the least resisted other damage type.
    public static class PoisonToTrueDamage {
        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(Character __instance, HitData hit, Character attacker) {
            if (hit == null || __instance == null || attacker != Player.m_localPlayer) {
                return;
            }

            var poison = hit.m_damage.m_poison;
            if (poison <= 0f) {
                return;
            }

            var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.PoisonToTrueDamage, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            var share = poison * Mathf.Clamp01(fraction);
            var mods = __instance.GetDamageModifiers();
            hit.m_damage.m_poison -= share;
            AddToLeastResisted(ref hit.m_damage, mods, share);
        }

        // Adds `amount` to whichever damage type the target resists least (highest effective multiplier),
        // considering every combat type except poison (the source) and the non-combat chop/pickaxe types.
        private static void AddToLeastResisted(ref HitData.DamageTypes damage, HitData.DamageModifiers mods, float amount) {
            var bestType = HitData.DamageType.Blunt;
            var bestScore = float.MinValue;

            void Consider(HitData.DamageType type) {
                var score = Effectiveness(mods.GetModifier(type));
                if (score > bestScore) {
                    bestScore = score;
                    bestType = type;
                }
            }

            Consider(HitData.DamageType.Blunt);
            Consider(HitData.DamageType.Slash);
            Consider(HitData.DamageType.Pierce);
            Consider(HitData.DamageType.Fire);
            Consider(HitData.DamageType.Frost);
            Consider(HitData.DamageType.Lightning);
            Consider(HitData.DamageType.Spirit);

            switch (bestType) {
                case HitData.DamageType.Blunt: damage.m_blunt += amount; break;
                case HitData.DamageType.Slash: damage.m_slash += amount; break;
                case HitData.DamageType.Pierce: damage.m_pierce += amount; break;
                case HitData.DamageType.Fire: damage.m_fire += amount; break;
                case HitData.DamageType.Frost: damage.m_frost += amount; break;
                case HitData.DamageType.Lightning: damage.m_lightning += amount; break;
                case HitData.DamageType.Spirit: damage.m_spirit += amount; break;
            }
        }

        // Relative damage-through for each resistance tier (higher = takes more). Immune/Ignore rank lowest
        // so we never funnel the converted damage into a type the enemy shrugs off entirely.
        private static float Effectiveness(HitData.DamageModifier modifier) {
            switch (modifier) {
                case HitData.DamageModifier.Immune:
                case HitData.DamageModifier.Ignore:
                    return 0f;
                case HitData.DamageModifier.VeryResistant:
                    return 0.25f;
                case HitData.DamageModifier.Resistant:
                    return 0.5f;
                case HitData.DamageModifier.SlightlyResistant:
                    return 0.75f;
                case HitData.DamageModifier.SlightlyWeak:
                    return 1.25f;
                case HitData.DamageModifier.Weak:
                    return 1.5f;
                case HitData.DamageModifier.VeryWeak:
                    return 2f;
                default:
                    return 1f;
            }
        }
    }
}
