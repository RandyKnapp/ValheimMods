using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.Magic.MagicItemEffects;

public class AutoMeads
{
    // True while AutoMeads is drinking for a hit that is about to drop the player to critical health. The
    // player's health is still above the threshold at that moment, so Instant Mead (ModifyMeads) reads this
    // as well as the current health -- otherwise the pre-emptive drink always came out as a slow mead.
    public static bool DrinkingForCriticalHit { get; private set; }

    // Invoked by SharedPlayerPostArmorDamagePatch (victim-side), once the hit has been through the block,
    // resistances and armor: auto-drink one healing mead when the hit will drop the local player to critical
    // health, or when they are already there.
    public static void OnIncomingHit(Character __instance, HitData hit)
    {
        if (__instance is not Player player ||
            player != Player.m_localPlayer ||
            !player.HasActiveMagicEffect(MagicEffectType.AutoMead) ||
            player.m_inventory == null ||
            !ModifyWithLowHealth.PlayerWillBecomeHealthCritical(player, hit))
        {
            return;
        }

        DrinkHealingMead(player);
    }

    // Damage over time (burning, poison, smoke) never reaches RPC_Damage or the hook above: the status effect
    // hands each tick straight to Character.ApplyDamage, so a player could burn or be poisoned to death with
    // Auto Mead and a mead in the bag. Each tick is checked here, before it lands.
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
    private static class Character_ApplyDamage_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Character __instance, HitData hit)
        {
            if (hit == null || __instance is not Player player || player != Player.m_localPlayer ||
                !IsDamageOverTime(hit.m_hitType) ||
                !player.HasActiveMagicEffect(MagicEffectType.AutoMead) ||
                player.m_inventory == null ||
                !ModifyWithLowHealth.PlayerWillBecomeHealthCriticalFromTick(player, hit))
            {
                return;
            }

            DrinkHealingMead(player);
        }
    }

    private static bool IsDamageOverTime(HitData.HitType hitType)
    {
        return hitType == HitData.HitType.Burning || hitType == HitData.HitType.Poisoned ||
            hitType == HitData.HitType.Smoke;
    }

    private static void DrinkHealingMead(Player player)
    {
        Inventory inventory = player.m_inventory;

        List<ItemDrop.ItemData> items = inventory.GetAllItemsOfType(ItemDrop.ItemData.ItemType.Consumable);
        foreach (ItemDrop.ItemData item in items)
        {
            if (HasHealthRegen(item) &&
                // Prevent the "cannot comsume this" message spam
                !player.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash()) &&
                !player.m_seman.HaveStatusEffectCategory(item.m_shared.m_consumeStatusEffect.m_category))
            {
                bool consumed;
                DrinkingForCriticalHit = true;
                try
                {
                    consumed = player.ConsumeItem(inventory, item);
                }
                finally
                {
                    DrinkingForCriticalHit = false;
                }

                // One mead per hit: meads from other mods that don't share vanilla's category would
                // otherwise all be drunk at once.
                if (consumed)
                {
                    return;
                }
            }
        }
    }

    public static bool HasHealthRegen(ItemDrop.ItemData itemData)
    {
        StatusEffect statusEffect = itemData.m_shared.m_consumeStatusEffect;

        if (statusEffect != null && statusEffect is SE_Stats seStats)
        {
            return (seStats.m_healthOverTime > 0 || seStats.m_healthUpFront > 0);
        }

        return false;
    }
}
