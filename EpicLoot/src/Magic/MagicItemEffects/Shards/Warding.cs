using Jotunn.Managers;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class Warding 
    {
        static GameObject sfx = null;
        static GameObject vfx = null;
        public static void UseMoreStaminaOnBlock(Humanoid __instance, HitData hit) 
        {
            var player = Player.m_localPlayer;
            if (!player.HasActiveMagicEffect("Warding")) return;
            float magicEffectValue = player.GetTotalActiveMagicEffectValue(MagicEffectType.Warding, .01f);
            float stamBlockPool = player.GetMaxStamina() * magicEffectValue;

            float blockDamageTaken = hit.GetTotalPhysicalDamage();

            float reduction = Math.Min(blockDamageTaken, stamBlockPool);

            if (player.GetStamina() < reduction) return;

            player.UseStamina(reduction); // use stam up to % of max stamina
            if (reduction > 5f) { // arbitrary amount to trigger fx. Its too flashy otherwise. Still might proc everytime
                if (sfx == null) {
                    sfx = PrefabManager.Instance.GetPrefab("sfx_wood_blocked_overlay");
                }
                if (vfx == null) {
                    vfx = PrefabManager.Instance.GetPrefab("vfx_MeadHasty");
                }
                if (sfx != null) {
                    GameObject.Instantiate(sfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
                }
                if (vfx != null) {
                    GameObject.Instantiate(vfx, player.m_visEquipment.m_leftHand.position, Quaternion.identity);
                }
            }

            float physicalDamageToMitigate = Math.Min(hit.m_damage.m_pierce, reduction);
            hit.m_damage.m_pierce -= physicalDamageToMitigate;
            reduction -= physicalDamageToMitigate;

            physicalDamageToMitigate = Math.Min(hit.m_damage.m_blunt, reduction);
            hit.m_damage.m_blunt -= physicalDamageToMitigate;
            reduction -= physicalDamageToMitigate;

            physicalDamageToMitigate = Math.Min(hit.m_damage.m_slash, reduction);
            hit.m_damage.m_slash -= physicalDamageToMitigate;
            reduction -= physicalDamageToMitigate;
        }
    }
}
