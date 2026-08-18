using System.Collections.Generic;
using EpicLoot.MagicItemEffects;

namespace EpicLoot.Magic.MagicItemEffects;

public class AutoMeads
{
    // Prefix handler invoked by CharacterDamageDispatch (victim-side): auto-drink a healing mead when the
    // incoming hit would drop the local player to critical health.
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

        Inventory inventory = player.m_inventory;

        List<ItemDrop.ItemData> items = inventory.GetAllItemsOfType(ItemDrop.ItemData.ItemType.Consumable);
        foreach (ItemDrop.ItemData item in items)
        {
            if (HasHealthRegen(item) &&
                // Prevent the "cannot comsume this" message spam
                !player.m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash()) &&
                !player.m_seman.HaveStatusEffectCategory(item.m_shared.m_consumeStatusEffect.m_category))
            {
                player.ConsumeItem(inventory, item);
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
