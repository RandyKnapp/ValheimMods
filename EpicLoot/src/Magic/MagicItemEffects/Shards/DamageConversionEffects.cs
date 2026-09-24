using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a conversion of incoming physical damage into elemental damage based on the player's active magic effects.
    public static class IncomingPhysicalConversion {
        // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier). The
        // dispatcher calls this before ModifyResistance so the converted element is then reduced by the
        // player's matching percentage resistance.
        //
        // With IsBlocked (from the BlockAttack postfix) it converts the on-block shares only. The general
        // shares were already converted by the RPC_Damage prefix, which runs before vanilla's BlockAttack on
        // this same HitData; adding them again converted a blocked hit twice (30% became 51%). The on-block
        // shares are meant as a share of the original physical damage, of which only (1 - general) is left
        // here, so they are scaled up to match.
        public static void ModifyIncoming(Character __instance, HitData hit, bool IsBlocked = false, bool IsParry = false) {
            if (__instance != Player.m_localPlayer) {
                return;
            }

            var player = Player.m_localPlayer;
            float toFire = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToFire, 0.01f);
            float toFrost = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToFrost, 0.01f);
            float toPoison = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToPoison, 0.01f);
            float toLightning = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToLightning, 0.01f);

            if (IsBlocked) {
                float remaining = 1f - Mathf.Min(1f, toFire + toFrost + toPoison + toLightning);
                if (remaining <= 0f) {
                    return;
                }

                toFire = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToFireOnBlock, 0.01f) / remaining;
                toFrost = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToFrostOnBlock, 0.01f) / remaining;
                toPoison = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToPoisonOnBlock, 0.01f) / remaining;
                toLightning = player.GetTotalActiveMagicEffectValue(MagicEffectType.PhysToLightningOnBlock, 0.01f) / remaining;
            }

            float total = toFire + toFrost + toPoison + toLightning;
            if (total <= 0f) {
                return;
            }

            float physical = hit.m_damage.m_blunt + hit.m_damage.m_slash + hit.m_damage.m_pierce;
            if (physical <= 0f) {
                return;
            }

            // Never convert more physical than the hit actually has.
            if (total > 1f) {
                float scale = 1f / total;
                toFire *= scale;
                toFrost *= scale;
                toPoison *= scale;
                toLightning *= scale;
                total = 1f;
            }

            hit.m_damage.m_fire += physical * toFire;
            hit.m_damage.m_frost += physical * toFrost;
            hit.m_damage.m_poison += physical * toPoison;
            hit.m_damage.m_lightning += physical * toLightning;

            DamageConversionHelper.RemovePhysicalShare(ref hit.m_damage, total);
        }
    }

    internal static class DamageConversionHelper {
        // Scales the three physical damage components down so `fraction` of the physical pool is
        // removed -- its magnitude having already been redistributed onto elements by the caller.
        internal static void RemovePhysicalShare(ref HitData.DamageTypes damage, float fraction) {
            float remaining = 1f - fraction;
            damage.m_blunt *= remaining;
            damage.m_slash *= remaining;
            damage.m_pierce *= remaining;
        }
    }
}
