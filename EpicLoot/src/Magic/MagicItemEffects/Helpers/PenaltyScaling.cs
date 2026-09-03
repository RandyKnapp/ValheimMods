using EpicLoot.Config;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Helpers for scaling shard effects by how heavy the player's loadout is, as measured by their net movement penalty.
    public static class PenaltyScaling {
        // Net movement penalty (as a positive fraction of base speed) treated as "fully committed" to a
        // heavy loadout, i.e. where MovementPenaltyFactor reaches 1. ~20% speed loss is roughly a full set
        // of the heaviest armor.
        //
        // Tunable as "MovementPenaltyReference" in the Global block of config/shardstones.json. It is a
        // plain static field rather than a per-read config lookup because MovementPenaltyFactor sits under
        // GetArmor / GetMaxCarryWeight / ModifyStaminaRegen, which run at 50Hz; EffectConfig.ApplyGlobalConfig
        // refreshes it once per config load instead. Seven shards scale off it, so moving it retunes all of
        // them together -- that is the point of it living in Global rather than on one effect.
        public const float DefaultMovementPenaltyReference = 0.40f;
        public static float MovementPenaltyReference = DefaultMovementPenaltyReference;

        // Config setup hook, called from EffectConfig.ApplyGlobalConfig. Clamped away from zero because
        // MovementPenaltyFactor divides by this.
        public static void RefreshGlobalConfig() {
            MovementPenaltyReference = Mathf.Max(0.001f,
                EffectConfig.Global("MovementPenaltyReference", DefaultMovementPenaltyReference));
        }

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

        // One measurement per player per simulation step. CurrentSpeed runs the entire status-effect
        // speed pipeline -- every status effect's ModifySpeed, plus the ApplyStatusEffectSpeedMods
        // postfixes our own shards and other mods install -- and the effects that scale by the penalty
        // sit on hot vanilla methods (GetMaxCarryWeight, GetArmor, GetDamage, ModifyStaminaRegen,
        // RaiseSkill) that are reached several times within one step. Those repeats were re-deriving a
        // number that could not have changed: every input CurrentSpeed reads (the equipment modifier
        // array, the status effect list, m_currentVel) is fixed for the duration of a step.
        //
        // Keyed on the frame *and* the fixed-step time because FixedUpdate runs more than once per
        // rendered frame below 50fps, and each of those physics steps is entitled to its own reading.
        private static Player _penaltyPlayer;
        private static int _penaltyFrame = -1;
        private static float _penaltyFixedTime = float.NaN;
        private static float _penaltyValue;

        // The player's net movement-speed penalty as a positive fraction of their reference speed
        // (e.g. 0.15 == moving at 85% of base jog speed). A net speed gain reads as 0, not a negative.
        public static float MovementPenalty(Player player) {
            var reference = ReferenceSpeed(player);
            if (reference <= 0f || _measuringSpeed) {
                return 0f;
            }

            var frame = Time.frameCount;
            var fixedTime = Time.fixedTime;
            var cached = _penaltyFrame == frame && _penaltyFixedTime == fixedTime
                && ReferenceEquals(_penaltyPlayer, player);

            // Null-conditional because the config entry is bound in Awake; nothing should reach this
            // before then, but a hot path is the wrong place to find out otherwise.
            if (cached && ELConfig.VerifyPenaltyScalingCache?.Value != true) {
                return _penaltyValue;
            }

            _measuringSpeed = true;
            try {
                var penalty = Mathf.Clamp01(1f - CurrentSpeed(player) / reference);

                if (cached) {
                    // Verification pass: the cached reading is still the one returned, so enabling this
                    // cannot change what the effects see -- it only reports when the two disagree.
                    if (!Mathf.Approximately(penalty, _penaltyValue)) {
                        EpicLoot.LogWarningForce(
                            $"PenaltyScaling: cached movement penalty {_penaltyValue} != recomputed {penalty} " +
                            $"(frame {frame}, fixedTime {fixedTime})");
                    }

                    return _penaltyValue;
                }

                // Only recorded on the measured path -- a re-entrant call bails at the guard above with
                // 0f, and that 0 must not be cached as this step's reading.
                _penaltyPlayer = player;
                _penaltyFrame = frame;
                _penaltyFixedTime = fixedTime;
                _penaltyValue = penalty;
                return penalty;
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
