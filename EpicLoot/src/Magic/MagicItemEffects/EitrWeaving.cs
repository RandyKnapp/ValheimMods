using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public static class EitrWeaving
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    private static class PatchParry_EitrWeave
    {
        private static void Postfix(Humanoid __instance, Character attacker)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            var eitrWeaveValue = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.EitrWeave);
            if (eitrWeaveValue > 0)
            {
                ItemDrop.ItemData currentBlocker = __instance.GetCurrentBlocker();
                bool parriedAttack = currentBlocker.m_shared.m_timedBlockBonus > 1f &&
                    __instance.m_blockTimer != -1f && __instance.m_blockTimer < 0.25f;
                    
                if (parriedAttack == false)
                {
                    return;
                }

                Player.m_localPlayer.AddEitr(eitrWeaveValue);
            }
        }
    }
}
