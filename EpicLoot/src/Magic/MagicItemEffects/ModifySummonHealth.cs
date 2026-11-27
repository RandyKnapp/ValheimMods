using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

// The bonus comes from the summon's stamp (ModifySummonDamage), not from whoever owns it now: vanilla resets an
// unhurt creature's max health whenever its owner loads it.
[HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
public class ModifySummonHealth
{
    public static void Prefix(Character __instance, ref float health)
    {
        if (!ModifySummonDamage.IsMagicSummon(__instance) || __instance.m_nview == null || !__instance.m_nview.IsValid())
        {
            return;
        }

        float bonus = __instance.m_nview.GetZDO().GetFloat(ModifySummonDamage.HealthZdoKey, 0f);
        if (bonus > 0f)
        {
            health *= 1f + bonus;
        }
    }
}
