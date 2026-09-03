using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Shared "pay blood to hold a block" trigger for the blood block shards (BloodBaseBlock,
    // BloodStaggerBlock). Each shard keeps its own Humanoid.UpdateBlock postfix so carrying both costs
    // blood twice, but the trigger logic lives here once: fire on the first frame of a block
    // (m_blockTimer is -1 while idle and is set to 0 on the block's first UpdateBlock pass) and charge
    // the local player a share of max health as untyped true damage, with the shared hit/blood fx.
    public static class BloodBlockSelfDamage {
        // Percent of max health charged per block start. Shared by both blood block shards, so it lives
        // in the Global block of config/shardstones.json ("BloodBlockSelfDamagePercent") rather than on
        // either effect. A plain static field because this sits on a per-frame UpdateBlock postfix;
        // EffectConfig.ApplyGlobalConfig refreshes it once per config load.
        public const float DefaultSelfDamagePercent = 5f;
        public static float SelfDamagePercent = DefaultSelfDamagePercent;

        // Config setup hook, called from EffectConfig.ApplyGlobalConfig. Clamped to 0..100: a negative
        // share would heal on block, and over 100% would one-shot the player on their first block.
        public static void RefreshGlobalConfig() {
            SelfDamagePercent = Mathf.Clamp(
                EffectConfig.Global("BloodBlockSelfDamagePercent", DefaultSelfDamagePercent), 0f, 100f);
        }

        private static GameObject sfx = null;
        private static GameObject vfx = null;

        public static void OnBlockStart(Humanoid instance, string effectType) {
            var player = Player.m_localPlayer;
            // UpdateBlock runs for every humanoid on this client -- blocking enemies included -- and on a
            // dedicated server there is no local player at all. Only the local player's own block start
            // may charge the local player.
            if (player == null || instance != player) {
                return;
            }

            if (!instance.IsBlocking() || instance.m_blockTimer != 0f) {
                return;
            }

            // Cached (EquipmentEffectCache-backed) overload: this runs from a per-frame patch, where the
            // parameterless string overload's full LINQ walk is too expensive.
            if (!player.HasActiveMagicEffect(effectType, out _)) {
                return;
            }

            HitData hit = new HitData();
            hit.SetAttacker(player); // self dmg as player. I want to trigger on hit effects.
                                     // Can scrap if its too powerful or jank. I expect this effect to go under utilized.

            // True damage: untyped damage does not run through armor or any known resistance.
            hit.m_damage.m_damage = player.GetMaxHealth() * (SelfDamagePercent / 100f);
            hit.m_staggerMultiplier = 0f;

            // addtions to validate hit
            hit.m_point = player.GetCenterPoint();
            hit.m_dir = Vector3.zero;
            hit.m_hitType = HitData.HitType.Self;
            hit.m_ignorePVP = true; // required to self dmg

            player.Damage(hit);

            if (sfx == null) {
                sfx = PrefabManager.Instance.GetPrefab("sfx_hit");
            }
            if (vfx == null) {
                vfx = PrefabManager.Instance.GetPrefab("vfx_BloodHit");
            }
            if (sfx != null) {
                GameObject.Instantiate(sfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
            }
            if (vfx != null) {
                GameObject.Instantiate(vfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
            }
        }
    }
}
