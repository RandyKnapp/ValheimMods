using EpicLoot.General;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class ModifySummonDamage
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Start))]
    public static class SetupSummonDamagePatch
    {
        public static void Postfix(Humanoid __instance)
        {
            // Setup the bonus damage for the summon when it is initially setup
            if (!__instance.IsPlayer() && Player.m_localPlayer != null)
            {
                if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifySummonDamage, out float effectValue, 0.01f))
                {
                    // If things arn't setup yet, don't do anything
                    if (__instance.m_nview.GetZDO() == null)
                    {
                        return;
                    }
                    __instance.m_nview.GetZDO().Set("el-msd", effectValue);

                    foreach (var item in __instance.m_inventory.GetAllItems())
                    {
                        if (item.GetDamage().EpicLootGetTotalDamage() > 0)
                        {
                            item.m_shared.m_attack.m_damageMultiplier += effectValue;
                            item.m_shared.m_secondaryAttack.m_damageMultiplier += effectValue;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GiveDefaultItems))]
    public static class ModifySummonDamage_Patch
    {
        public static void Postfix(Humanoid __instance)
        {
            if (__instance.IsPlayer() || __instance.m_nview == null || __instance.m_nview.GetZDO() == null)
            {
                return;
            }

            // Apply Damage modification to all items in the inventory of the summon
            float summonDamageBonus = __instance.m_nview.GetZDO().GetFloat("el-msd", 0f);

            if (summonDamageBonus > 0f && !__instance.IsPlayer())
            {
                foreach (var item in __instance.m_inventory.GetAllItems())
                {
                    if (item.GetDamage().EpicLootGetTotalDamage() > 0)
                    {
                        item.m_shared.m_attack.m_damageMultiplier += summonDamageBonus;
                        item.m_shared.m_secondaryAttack.m_damageMultiplier += summonDamageBonus;
                    }
                }
            }
        }
    }
}