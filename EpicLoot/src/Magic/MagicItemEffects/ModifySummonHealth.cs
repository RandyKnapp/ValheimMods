using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch(typeof(Character), nameof(Character.SetMaxHealth))]
public class ModifySummonHealth
{
    public static void Prefix(Character __instance, ref float health)
    {
        if (!__instance.IsTamed())
        {
            return;
        }

        Tameable isTamable = __instance.GetComponent<Tameable>();
        if (isTamable == null)
        {
            return;
        }

        if (isTamable.m_levelUpOwnerSkill == Skills.SkillType.BloodMagic || isTamable.m_levelUpOwnerSkill == Skills.SkillType.ElementalMagic)
        {
            if (Player.m_localPlayer != null && Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifySummonHealth, out float effectValue, 0.01f))
            {
                health *= (1 + effectValue);
            }
        }
    }
}