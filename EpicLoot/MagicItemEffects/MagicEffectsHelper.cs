using System.Linq;

namespace EpicLoot.MagicItemEffects
{
    public static class MagicEffectsHelper
    {
        public static float GetTotalActiveMagicEffectValue(Player player, ItemDrop.ItemData itemData, string effectType, float scale = 1.0f)
        {
            return GetTotalActiveMagicEffectValue(player, itemData.GetMagicItem(), effectType, scale);
        }

        public static float GetTotalActiveMagicEffectValue(Player player, MagicItem magicItem, string effectType, float scale = 1.0f)
        {
            if (player != null)
            {
                return player.GetTotalActiveMagicEffectValue(effectType, scale);
            }
            else if (magicItem != null)
            {
                return magicItem.GetTotalEffectValue(effectType, scale);
            }

            return 0;
        }

        private static bool IsWeapon(ItemDrop.ItemData itemData)
        {
            if (itemData == null)
                return false;

            switch (itemData.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Torch:
                    return true;
                default:
                    return false;
            }
        }

        private static ItemDrop.ItemData GetIgnoreWeapon(Player player, ItemDrop.ItemData equippedWeapon)
        {
            if (player.m_rightItem == equippedWeapon && IsWeapon(player.m_leftItem))
                return player.m_leftItem;
            if (player.m_leftItem == equippedWeapon && IsWeapon(player.m_rightItem))
                return player.m_rightItem;

            return null;
        }

        public static float GetTotalActiveMagicEffectValueForWeapon(Player player, ItemDrop.ItemData itemData, string effectType, float scale = 1.0f)
        {
            if (player != null)
                return player.GetTotalActiveMagicEffectValue(effectType, scale, GetIgnoreWeapon(player, itemData));
            if (itemData.IsMagic(out var magicItem))
                return magicItem.GetTotalEffectValue(effectType, scale);
            return 0;
        }

        public static bool HasActiveMagicEffect(Player player, ItemDrop.ItemData itemData, string effectType)
        {
            return HasActiveMagicEffect(player, itemData.GetMagicItem(), effectType);
        }

        public static bool HasActiveMagicEffect(Player player, MagicItem magicItem, string effectType)
        {
            if (player != null)
                return player.HasActiveMagicEffect(effectType);
            else if (magicItem != null)
                return magicItem.HasEffect(effectType);
            return false;
        }

        public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData itemData, string effectType)
        {
            if (player != null)
                return player.HasActiveMagicEffect(effectType, GetIgnoreWeapon(player, itemData));
            else if (itemData.IsMagic(out var magicItem))
                return magicItem.HasEffect(effectType);
            return false;
        }

        public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale = 1.0f)
        {
            var setEffects = player.GetAllActiveSetMagicEffects(MagicEffectType.ModifyArmor);
            return setEffects.Count > 0 ? scale * setEffects.Sum(x => x.EffectValue) : 0;
        }
    }
}
