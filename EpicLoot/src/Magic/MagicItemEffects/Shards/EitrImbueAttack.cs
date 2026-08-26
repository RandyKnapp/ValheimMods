namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to spirit damage based on the physical damage dealt by the player, at the cost of Eitr
    public static class EitrImbueAttack {
        // Eitr paid per point of bonus spirit damage, conversion ratio
        private const float EitrCostPerDamage = 1f;

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (attacker is not Player player || player != Player.m_localPlayer) {
                return;
            }

            // The shard is socketed into the attacking weapon, so read the effect from that weapon
            // rather than player-wide -- the imbue only fires for the weapon that carries it.
            // GetActiveWeapon resolves the weapon of the attack in flight; GetCurrentWeapon returned
            // the right hand first, so an off-hand weapon's shard never fired.
            // TODO: make this work for unarmed attacks and not be required on the weapon itself
            var magicItem = global::EpicLoot.src.Magic.MagicItemEffects.Helpers.MagicEffectsHelper.GetActiveWeapon(player)?.GetMagicItem();
            float bonus = GetSpiritBonus(magicItem, hit.m_damage);
            if (bonus <= 0f) {
                return;
            }

            // No bonus unless the pool can fully cover the cost.
            float cost = bonus * EitrCostPerDamage;
            if (player.GetEitr() < cost) {
                return;
            }

            player.UseEitr(cost);
            hit.m_damage.m_spirit += bonus;
        }

        public static float GetSpiritBonus(MagicItem magicItem, HitData.DamageTypes damage) {
            if (magicItem == null ||
                !magicItem.HasEffect(MagicEffectType.EitrImbueAttack, includeSocketed: true)) {
                return 0f;
            }

            float fraction = magicItem.GetTotalEffectValue(MagicEffectType.EitrImbueAttack, 0.01f);
            float physical = damage.m_blunt + damage.m_slash + damage.m_pierce;
            float bonus = physical * fraction;
            return bonus > 0f ? bonus : 0f;
        }
    }
}
