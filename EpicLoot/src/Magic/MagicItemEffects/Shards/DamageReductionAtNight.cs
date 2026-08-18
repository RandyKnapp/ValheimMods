using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Reduces incoming damage at night
    public static class DamageReductionAtNight {
        // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier).
        public static void ModifyIncoming(Character __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer || !EnvMan.IsNight()) {
                return;
            }

            var reduction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DamageReductionAtNight, 0.01f);
            if (reduction > 0f) {
                hit.m_damage.Modify(1f - Mathf.Clamp01(reduction));
            }
        }
    }
}
