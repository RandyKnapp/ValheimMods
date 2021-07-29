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

        public static bool HasActiveMagicEffect(Player player, ItemDrop.ItemData itemData, string effectType)
        {
            return HasActiveMagicEffect(player, itemData.GetMagicItem(), effectType);
        }

        public static bool HasActiveMagicEffect(Player player, MagicItem magicItem, string effectType)
        {
            if (player != null)
            {
                return player.HasActiveMagicEffect(effectType);
            }
            else if (magicItem != null)
            {
                return magicItem.HasEffect(effectType);
            }

            return false;
        }

        public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale = 1.0f)
        {
            var setEffects = player.GetAllActiveSetMagicEffects(MagicEffectType.ModifyArmor);
            return setEffects.Count > 0 ? scale * setEffects.Sum(x => x.EffectValue) : 0;
        }
    }
}
