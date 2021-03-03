using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
    [HarmonyPatch(typeof(SEMan), "ModifyMaxCarryWeight")]
    public static class AddCarryWeight_SEMan_ModifyMaxCarryWeight_Patch
    {
        public static void Postfix(SEMan __instance, ref float limit)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.AddCarryWeight);
                foreach (var item in items)
                {
                    var carryWeightBonus = item.GetMagicItem().GetTotalEffectValue(MagicEffectType.AddCarryWeight);
                    limit += carryWeightBonus;
                }
            }
        }
    }
}
