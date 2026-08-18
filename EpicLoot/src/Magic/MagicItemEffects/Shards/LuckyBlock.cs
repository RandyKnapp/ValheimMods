using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot.MagicItemEffects.Shards {
    // Golden shield: a chance for any successful block to stagger the attacker outright, as though the
    // player had parried it.
    //
    // This is a new on-block trigger family. The three existing ones all act on the blocker or on the
    // incoming hit; this is the first that reaches back out and touches the *attacker*.
    //
    // Multiplayer: Humanoid.BlockAttack is called from inside Character.RPC_Damage, after its IsOwner()
    // gate, so this handler already runs on the blocking player's own client -- the local-player guard is
    // correct and fires no matter who owns the attacker. Character.Stagger is then the sanctioned way to
    // act on that attacker: it calls RPC_Stagger directly when we own them and routes an RPC to their
    // owner otherwise, so nothing here ever writes to a ZDO we don't own.
    public static class LuckyBlock {
        // Postfix handler invoked by SharedHumanoidBlockAttackPatch (block succeeded).
        public static void OnBlock(Humanoid blocker, HitData hit, Character attacker) {
            if (hit == null || attacker == null) {
                return;
            }

            if (!(blocker is Player player) || player != Player.m_localPlayer || attacker == player) {
                return;
            }

            // The same gate vanilla uses before its own parry stagger (Humanoid.BlockAttack): creatures
            // flagged not to stagger when blocked are immune to this too.
            if (!attacker.m_staggerWhenBlocked) {
                return;
            }

            // RPC_Stagger no-ops while the target is already staggering. Bailing early saves a pointless
            // network round trip, and incidentally de-duplicates the case where this rolls on a perfect
            // block that vanilla has already staggered.
            if (attacker.IsStaggering()) {
                return;
            }

            var chance = player.GetTotalActiveMagicEffectValue(MagicEffectType.LuckyBlock, 0.01f);
            if (chance <= 0f || Random.value >= chance) {
                return;
            }

            // Negated so the attacker turns to face the blocker, matching vanilla's parry stagger.
            attacker.Stagger(-hit.m_dir);
        }
    }
}
