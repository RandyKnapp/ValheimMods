using System;
using EpicLoot.Healing;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public static class AddLifeSteal
    {
        [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
        public static class AddLifeSteal_Character_Damage_Patch
        {
            public static void Postfix(HitData hit)
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

                var attacker = hit.GetAttacker() as Humanoid;
                if (attacker == null)
                {
                    return;
                }

                // TODO track actual weapon which made a hit for better life-steal calculation
                var weapon = attacker.GetCurrentWeapon();

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