using System;
using EpicLoot.Healing;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class AddLifeSteal
    {
        // when Player or Humanoid is hit
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnDamaged))]
        public static class AddLifeSteal_Humanoid_OnDamaged_Patch
        {
            private static void Postfix(HitData hit)
            {
                CheckAndDoLifeSteal(hit);
            }
        }
        
        // when other creature is hit
        [HarmonyPatch(typeof(Character), nameof(Character.OnDamaged))]
        public static class AddLifeSteal_Character_OnDamaged_Patch
        {
            private static void Postfix(HitData hit)
            {
                CheckAndDoLifeSteal(hit);
            }
        }
        
        public static void CheckAndDoLifeSteal(HitData hit)
        {
            try
            {
                if (!hit.HaveAttacker())
                {
                    return;
                }

                var attacker = hit.GetAttacker();
                if (attacker is Humanoid == false) return;

                var attackerHumanoid = (Humanoid) attacker;
                // TODO track actual weapon which made a hit for better life-steal calculation
                var weapon = attackerHumanoid.GetCurrentWeapon();

                // in case weapon's durability is destroyed after hit?
                // OR in case damage is delayed and player hides weapon - see to-do above
                if (weapon == null || !weapon.IsMagic() || !weapon.HasMagicEffect(MagicEffectType.LifeSteal))
                {
                    return;
                }

                var lifeStealMultiplier = weapon.GetMagicItem().GetTotalEffectValue(MagicEffectType.LifeSteal, 0.01f);
                var healOn = hit.m_damage.GetTotalDamage() * lifeStealMultiplier;
                
                EpicLoot.Log("lifesteal " + healOn);
                var healFromQueue = false;
                if (attacker.IsPlayer())
                {
                    var healingQueue = attacker.GetComponent<HealingQueueMono>();
                    if (healingQueue)
                    {
                        healFromQueue = true;
                        healingQueue.HealRequests.Add(healOn);
                    }
                } 
                
                if (!healFromQueue)
                {
                    // mostly for NPC with lifeSteal weapon
                    attacker.Heal(healOn);
                }
            }
            catch (Exception e)
            {
                EpicLoot.LogError(e.Message);
            }
        }
    }
}