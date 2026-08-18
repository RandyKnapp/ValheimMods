using Jotunn.Managers;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ElementalWarding {
        static GameObject fx = null;
        public static void UseEitrOnBlock(Humanoid __instance, HitData hit) {
            var player = Player.m_localPlayer;
            if (!player.HasActiveMagicEffect("ElementalWarding")) return;
            float magicEffectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.ElementalWarding, .01f);
            float eitrBlockPool = player.GetMaxEitr() * magicEffectValue;

            float blockDamageTaken = hit.GetTotalElementalDamage();

            float reduction = Math.Min(blockDamageTaken, eitrBlockPool);

            if (player.GetEitr() < reduction) return;

            player.UseEitr(reduction);

            if (reduction > 5f) { // arbitrary amount to trigger fx. Its too flashy otherwise. Still might proc everytime
                if (fx == null) {
                    fx = PrefabManager.Instance.GetPrefab("fx_StaffShield_Hit");
                }
                if (fx != null) {
                    GameObject.Instantiate(fx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
                }
            }

            float elementalDamageToMitigate = Math.Min(hit.m_damage.m_fire, reduction);
            hit.m_damage.m_fire -= elementalDamageToMitigate;
            reduction -= elementalDamageToMitigate;

            elementalDamageToMitigate = Math.Min(hit.m_damage.m_frost, reduction);
            hit.m_damage.m_frost -= elementalDamageToMitigate;
            reduction -= elementalDamageToMitigate;

            elementalDamageToMitigate = Math.Min(hit.m_damage.m_lightning, reduction);
            hit.m_damage.m_lightning -= elementalDamageToMitigate;
            reduction -= elementalDamageToMitigate;
        }
    }
}
