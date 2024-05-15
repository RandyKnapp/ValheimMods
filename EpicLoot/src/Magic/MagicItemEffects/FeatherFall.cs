using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    //UpdateEquipmentStatusEffects
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateEquipmentStatusEffects))]
    public class FeatherFall_Humanoid_UpdateEquipmentStatusEffects_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Humanoid __instance)
        {
            if (__instance is Player player)
            {
                var slowFall = ObjectDB.instance.GetStatusEffect("SlowFall".GetHashCode());
                if (slowFall == null)
                {
                    EpicLoot.LogError("Could not find SlowFall status effect!");
                    return;
                }
                
                EquipmentEffectCache.Reset(player);

                var shouldHaveFeatherFall = player.HasActiveMagicEffect(MagicEffectType.FeatherFall);
                var hasFeatherFall = player.m_equipmentStatusEffects.Contains(slowFall);
                if (!hasFeatherFall && shouldHaveFeatherFall)
                {
                    player.m_equipmentStatusEffects.Add(slowFall);
                    player.m_seman.AddStatusEffect(slowFall);
                    var equipEffectsList = string.Join(", ", player.m_equipmentStatusEffects.Select(x => x.name));
                    EpicLoot.Log($"Adding feather fall. Current equip effects: {equipEffectsList}");
                }
            }
        }
    }
}