using System.Linq;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
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
                return magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
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
            {
                return player.GetTotalActiveMagicEffectValue(effectType, scale, GetIgnoreWeapon(player, itemData));
            }
            else if (itemData.IsMagic(out var magicItem))
            {
                return magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
            }

            return 0;
        }

        public static bool HasActiveMagicEffect(Player player, ItemDrop.ItemData itemData, string effectType, out float effectValue)
        {
            return HasActiveMagicEffect(player, itemData.GetMagicItem(), effectType, out effectValue);
        }

        public static bool HasActiveMagicEffect(Player player, MagicItem magicItem, string effectType, out float effectValue)
        {
            effectValue = 0f;
            if (player != null)
            {
                return player.HasActiveMagicEffect(effectType, out effectValue);
            }
            else if (magicItem != null)
            {
                return magicItem.HasEffect(effectType, includeSocketed: true);
            }
            return false;
        }

        public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData itemData, string effectType,
            out float effectValue, float scale = 1.0f)
        {
            effectValue = 0f;
            if (player != null)
            {
                return player.HasActiveMagicEffect(effectType, out effectValue, scale, GetIgnoreWeapon(player, itemData));
            }
            else if (itemData.IsMagic(out var magicItem))
            {
                effectValue = magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
                return magicItem.HasEffect(effectType, includeSocketed: true);
            }
            return false;
        }

        public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale = 1.0f)
        {
            var setEffects = player.GetAllActiveSetMagicEffects(effectType);
            return setEffects.Count > 0 ? scale * setEffects.Sum(x => x.EffectValue) : 0;
        }

        // --- Shared guards/lookups for effect dispatchers -------------------------------------------
        // These fold the boilerplate that nearly every on-hit / on-damage effect repeats. Used by the
        // Character.Damage / Character.RPC_Damage dispatchers (see Dispatch/) so each effect's handler
        // can assume the guard already passed and just do its work.

        /// <summary>
        /// True when <paramref name="hit"/> is an outgoing hit dealt by the local player (attacker side).
        /// Runs once, on the attacker's client, before Character.Damage forwards to the target.
        /// </summary>
        public static bool IsLocalOutgoingHit(HitData hit, out Player player)
        {
            player = Player.m_localPlayer;
            return hit != null && player != null && hit.GetAttacker() == player;
        }

        /// <summary>
        /// True when <paramref name="instance"/> is the local player receiving damage / owning regen.
        /// </summary>
        public static bool IsLocalVictim(Character instance)
        {
            return instance != null && instance == Player.m_localPlayer;
        }

        /// <summary>
        /// Null-safe read of a whole-number-percent effect summed across the local player's equipment
        /// (shard values are authored as percents, hence the default 0.01f scale).
        /// </summary>
        public static float GetLocalPercent(string effectType, float scale = 0.01f)
        {
            var player = Player.m_localPlayer;
            return player != null ? player.GetTotalActiveMagicEffectValue(effectType, scale) : 0f;
        }

        /// <summary>
        /// The weapon that produced the current attack: the in-progress melee attack's weapon when one is
        /// active (<see cref="Attack_Patch.ActiveAttack"/>), otherwise the player's currently equipped
        /// weapon. Used by on-hit effects socketed into the attacking weapon.
        /// </summary>
        public static ItemDrop.ItemData GetActiveWeapon(Player player)
        {
            return Attack_Patch.ActiveAttack?.m_weapon ?? player?.GetCurrentWeapon();
        }
    }
}
