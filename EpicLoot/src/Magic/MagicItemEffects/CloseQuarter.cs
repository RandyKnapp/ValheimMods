using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

// Close Quarter: the weapon's hits knock enemies back less, so they stay in reach of the next swing.
// Only the pushback is reduced; damage and stagger are untouched.
public static class CloseQuarter
{
    // Prefix handler invoked by SharedCharacterDamagePatch (attacker-side outgoing modifier). Character.Damage
    // serializes the hit to the target's owner, whose RPC_Damage applies the pushback, so the reduced force
    // reaches it whoever owns the target.
    public static void ModifyOutgoingHit(HitData hit, Character attacker)
    {
        if (hit.m_pushForce <= 0f || !(attacker is Player player) || player != Player.m_localPlayer)
        {
            return;
        }

        float reduction = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, MagicEffectsHelper.GetActiveWeapon(player), MagicEffectType.CloseQuarter, 0.01f);
        if (reduction > 0f)
        {
            hit.m_pushForce *= 1f - Mathf.Clamp01(reduction);
        }
    }

    // The item's knockback stat with this item's own Close Quarter applied, for the tooltip.
    public static float GetKnockback(ItemDrop.ItemData item, MagicItem magicItem)
    {
        float reduction = magicItem?.GetTotalEffectValue(MagicEffectType.CloseQuarter, 0.01f) ?? 0f;
        return item.m_shared.m_attackForce * (1f - Mathf.Clamp01(reduction));
    }
}
