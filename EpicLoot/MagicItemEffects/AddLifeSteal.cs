using System;
using EpicLoot.HealingQueue;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class AddLifeSteal
    {

        /// when Player or Humanoid is hit
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.OnDamaged))]
        public static class AddLifeSteal_Humanoid_OnDamaged_Patch
        {
            private static void Postfix(HitData hit)
            {
                LifeStealHelper.CheckAndDoLifeSteal(hit);
            }
        }
        
        /// when other creature is hit
        [HarmonyPatch(typeof(Character), nameof(Character.OnDamaged))]
        public static class AddLifeSteal_Character_OnDamaged_Patch
        {
            private static void Postfix(HitData hit)
            {
                LifeStealHelper.CheckAndDoLifeSteal(hit);
            }
        }
        
        public static class LifeStealHelper
        {
            public static void CheckAndDoLifeSteal(HitData hit)
            {
                try
                {
                    if (!hit.HaveAttacker()) return;

                    Character attacker = hit.GetAttacker();
                    if (attacker is Humanoid == false) return;

                    Humanoid attackerHumanoid = (Humanoid) attacker;
                    // TODO track actual weapon which made a hit for better life-steal calculation
                    ItemDrop.ItemData weapon = attackerHumanoid.GetCurrentWeapon();

                    // in case weapon's durability is destroyed after hit?
                    // OR in case damage is delayed and player hides weapon - see to-do above
                    if (weapon == null) return;

                    if (!weapon.HasMagicEffect(MagicEffectType.LifeSteal)) return;

                    var lifeStealMlt = weapon.Extended().GetMagicItem().GetTotalEffectValue(
                                    MagicEffectType.LifeSteal, 0.01f);
                    float healOn = hit.m_damage.GetTotalDamage() * lifeStealMlt;
                    
                    Debug.Log("lifesteal " + healOn);
                    bool healFromQueue = false;
                    if (attacker is Player)
                    {
                        var healingQueue = attacker.GetComponent<HealingQueueMono>();
                        if (healingQueue)
                        {
                            healFromQueue = true;
                            healingQueue.healRequests.Add(healOn);
                        }
                    } 
                    
                    if (!healFromQueue)
                    {
                        // mostly for NPC with lifeSteal weapon
                        attacker.Heal(healOn);
                    }
                } catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}