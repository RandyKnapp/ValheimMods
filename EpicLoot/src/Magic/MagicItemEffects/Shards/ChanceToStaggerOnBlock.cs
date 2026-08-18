using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ChanceToStaggerOnBlock {
        public static void StaggerOnBlock(Character attacker)
        {
            var player = Player.m_localPlayer;

            // Same guards as LuckyBlock: the attacker may have despawned mid-swing, and when an enemy
            // blocks the local player's own hit the attacker IS the player -- without the self check the
            // shard would stagger its owner. Creatures flagged not to stagger when blocked are immune to
            // this too, and an already-staggering attacker needs no second stagger.
            if (player == null || attacker == null || attacker == player) return;
            if (!attacker.m_staggerWhenBlocked || attacker.IsStaggering()) return;

            float staggerChance = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaggerOnBlock, .01f);
            float staggerRoll = Random.Range(0f, 1f);

            if (staggerChance > staggerRoll)
            {
                attacker.Stagger(Vector3.zero); // relative to player or attacker would be intentional choice. Parries are relative
                                                // to player in vanilla so that staggered enemies face player on parry.
                                                // making this stagger in place so aoe attacks leave attackers staggering in place
            }
        }
    }
}