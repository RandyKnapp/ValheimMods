using EpicLoot.General;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Absorbs a portion of incoming damage by spending Eitr
    public static class EitrShield {
        static GameObject effect = null;
        // Prefix handler invoked by CharacterRpcDamageDispatch (victim-side incoming modifier; runs after
        // avoidance so a fully-avoided hit never spends eitr).
        public static void ModifyIncoming(Character __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer) {
                return;
            }

            float fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.EitrShield, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            float eitr = Player.m_localPlayer.GetEitr();
            if (eitr <= 0f) {
                return;
            }

            float total = hit.m_damage.EpicLootGetTotalDamageAgainstPlayer();
            if (total <= 0f) {
                return;
            }

            float absorb = Mathf.Min(total * fraction, eitr);
            if (absorb <= 0f) {
                return;
            }

            Player.m_localPlayer.UseEitr(absorb);
            hit.m_damage.Modify(1f - absorb / total);
            if (effect == null) {
                effect = PrefabManager.Instance.GetPrefab("fx_GoblinShieldHit");
            }
            if (effect != null) {
                GameObject.Instantiate(effect, __instance.transform.position, Quaternion.identity);
            }
        }
    }
}
