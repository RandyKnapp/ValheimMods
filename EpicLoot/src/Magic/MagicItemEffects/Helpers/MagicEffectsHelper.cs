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
                if (itemData != null && !player.IsItemEquiped(itemData))
                {
                    // The player-wide total already stands the firing weapon in for the held ones.
                    return itemData == HitSource.FiringWeapon
                        ? player.GetTotalActiveMagicEffectValue(effectType, scale)
                        : player.GetTotalActiveMagicEffectValue(effectType, scale) +
                            GetStandInAdjustment(player, itemData, effectType, scale);
                }

                return player.GetTotalActiveMagicEffectValue(effectType, scale, GetIgnoreWeapon(player, itemData));
            }
            else if (itemData.IsMagic(out var magicItem))
            {
                return magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true);
            }

            return 0;
        }

        /// <summary>
        /// What to add to the player's equipped-gear total while a thrown weapon's projectile is landing. Vanilla
        /// unequips a thrown weapon as it leaves the hand, so its effects had dropped out of every total by the
        /// time it hit; for the duration of its hit it counts in place of the weapons held now, the way it
        /// counted when it was thrown. Zero at all other times, including for an arrow (the bow stays equipped).
        /// </summary>
        public static float GetFiringWeaponAdjustment(Player player, string effectType, float scale)
        {
            ItemDrop.ItemData firing = HitSource.FiringWeapon;
            if (firing == null || player != Player.m_localPlayer || player.IsItemEquiped(firing))
            {
                return 0f;
            }

            return GetStandInAdjustment(player, firing, effectType, scale);
        }

        // An unequipped weapon counted in place of the weapons held now: its own value, less theirs. Also what
        // an inventory tooltip for an unequipped weapon should show.
        private static float GetStandInAdjustment(Player player, ItemDrop.ItemData weapon, string effectType, float scale)
        {
            float adjustment = GetOwnEffectValue(weapon, effectType, scale);
            if (IsWeapon(player.m_rightItem))
            {
                adjustment -= GetOwnEffectValue(player.m_rightItem, effectType, scale);
            }
            if (IsWeapon(player.m_leftItem))
            {
                adjustment -= GetOwnEffectValue(player.m_leftItem, effectType, scale);
            }

            return adjustment;
        }

        private static float GetOwnEffectValue(ItemDrop.ItemData item, string effectType, float scale)
        {
            return item != null && item.IsMagic(out MagicItem magicItem)
                ? magicItem.GetTotalEffectValue(effectType, scale, includeSocketed: true)
                : 0f;
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
                effectValue = GetTotalActiveMagicEffectValueForWeapon(player, itemData, effectType, scale);
                return effectValue != 0f;
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
        /// The weapon that produced the current attack: the weapon that fired the projectile or area effect
        /// now dealing damage (<see cref="HitSource.FiringWeapon"/>), else the in-progress melee attack's weapon
        /// (<see cref="Attack_Patch.ActiveAttack"/>), otherwise the player's currently equipped weapon. Used by
        /// on-hit effects socketed into the attacking weapon.
        /// </summary>
        public static ItemDrop.ItemData GetActiveWeapon(Player player)
        {
            return HitSource.FiringWeapon ?? Attack_Patch.ActiveAttack?.m_weapon ?? player?.GetCurrentWeapon();
        }

        /// <summary>
        /// Whether <paramref name="hit"/> killed <paramref name="target"/>, for a Character.Damage postfix on the
        /// attacker's client. When this client owns the target, Character.Damage has already run RPC_Damage
        /// synchronously (ZRoutedRpc handles a call to its own peer immediately), so the health is post-hit and
        /// is read as-is -- subtracting the hit again refunded Wager on hits that left the target alive. A
        /// remote target has not taken the hit yet, so its owner's resistance, armor and difficulty scaling are
        /// estimated here; fire, poison and spirit arrive as damage over time and never kill on the hit itself.
        /// </summary>
        public static bool IsLethalHit(Character target, HitData hit)
        {
            if (target == null || hit == null || target.m_nview == null || !target.m_nview.IsValid())
            {
                return false;
            }

            if (target.m_nview.IsOwner())
            {
                return target.GetHealth() <= 0f || target.IsDead();
            }

            HitData estimate = hit.Clone();
            estimate.ApplyResistance(target.GetDamageModifiers(), out _);
            if (!target.IsPlayer())
            {
                if (target.IsStaggering())
                {
                    estimate.ApplyModifier(2f);
                }
                if (Game.m_worldLevel > 0)
                {
                    estimate.ApplyArmor(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseAC);
                }
                estimate.ApplyModifier(Game.instance.GetDifficultyDamageScaleEnemy(target.transform.position));
                estimate.ApplyModifier(Game.m_playerDamageRate);
            }

            HitData.DamageTypes damage = estimate.m_damage;
            float instantDamage = damage.GetTotalDamage() - damage.m_fire - damage.m_poison - damage.m_spirit;
            return target.GetHealth() - instantDamage <= 0f;
        }
    }
}
