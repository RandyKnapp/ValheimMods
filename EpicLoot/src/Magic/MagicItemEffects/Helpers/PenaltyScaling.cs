using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Helpers for scaling shard effects by how heavy the player's loadout is, as measured by their net movement penalty.
    public static class PenaltyScaling {
        // Net movement penalty (as a positive fraction of base speed) treated as "fully committed" to a
        // heavy loadout, i.e. where MovementPenaltyFactor reaches 1. ~20% speed loss is roughly a full set
        // of the heaviest armor.
        public const float MovementPenaltyReference = 0.40f;

        // How loaded the player's pack is: 0 (empty) .. 1 (at or over the carry cap).
        public static float WeightFactor(Player player) {
            if (player == null) {
                return 0f;
            }

            var maxCarry = Mathf.Max(1f, player.GetMaxCarryWeight());
            return Mathf.Clamp01(player.GetInventory().GetTotalWeight() / maxCarry);
        }

        // Guards MovementPenalty against re-entering itself. It runs the status-effect speed pipeline,
        // and its callers hook GetArmor / ModifyMaxCarryWeight / GetDamage -- so a StatusEffect.ModifySpeed
        // (ours or another mod's) that reads armor or carry weight would otherwise recurse forever.
        private static bool _measuringSpeed;

        // The player's reference movement speed: the unmodified base jog speed off the Player prefab, before
        // any gear, enchant or status-effect modifier. Character.UpdateWalking starts from this same value.
        public static float ReferenceSpeed(Player player) {
            return player == null ? 0f : player.m_speed;
        }

        // The player's current *net* jog speed, i.e. ReferenceSpeed run through the same modifier chain
        // Character.UpdateWalking uses for the jog case:
        //   m_speed * GetJogSpeedFactor()  ->  SEMan.ApplyStatusEffectSpeedMods
        // GetJogSpeedFactor is protected, but it is just 1 + GetEquipmentMovementModifier(). Reading that
        // (rather than the raw equipment modifier at index 0) is what makes this net: EpicLoot's own
        // ModifyMovementSpeed / RemoveSpeedPenalty postfix lands there, as do the shard speed bonuses that
        // hook ApplyStatusEffectSpeedMods (TravelLight, BurningSpeed, StormRider) and every vanilla speed
        // status effect. Offset your armor's penalty with speed bonuses and the penalty shards stop paying.
        // The situational terms UpdateWalking layers on (walk/run/crouch, attack slowdown, liquid, lava) are
        // deliberately left out -- those describe what the player is doing, not how heavy their loadout is.
        public static float CurrentSpeed(Player player) {
            if (player == null) {
                return 0f;
            }

            var speed = player.m_speed * (1f + player.GetEquipmentMovementModifier());
            if (player.m_seman != null) {
                player.m_seman.ApplyStatusEffectSpeedMods(ref speed, player.m_currentVel);
            }

            return Mathf.Max(0f, speed);
        }

        // The player's net movement-speed penalty as a positive fraction of their reference speed
        // (e.g. 0.15 == moving at 85% of base jog speed). A net speed gain reads as 0, not a negative.
        public static float MovementPenalty(Player player) {
            var reference = ReferenceSpeed(player);
            if (reference <= 0f || _measuringSpeed) {
                return 0f;
            }

            _measuringSpeed = true;
            try {
                return Mathf.Clamp01(1f - CurrentSpeed(player) / reference);
            } finally {
                _measuringSpeed = false;
            }
        }

        // MovementPenalty normalized to 0..1 against MovementPenaltyReference, for effects that scale a
        // shard value by "how heavy" the loadout is.
        public static float MovementPenaltyFactor(Player player) {
            return Mathf.Clamp01(MovementPenalty(player) / MovementPenaltyReference);
        }
    }
}
