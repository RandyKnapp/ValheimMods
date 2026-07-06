using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class ModifyWispRange
    {
        public const string DEMISTER_ZDO = "el-demist";

        public static void SetDemister(ref Demister demister, float magicEffectValue)
        {
            if (demister == null || magicEffectValue <= 0f)
            {
                return;
            }

            var forceField = demister.GetComponent<ParticleSystemForceField>();
            if (forceField == null)
            {
                return;
            }

            //EpicLoot.LogErrorForce("Setting Demister range multiplier!");
            forceField.endRange *= (1 + magicEffectValue);
        }
    }

    [HarmonyPatch(typeof(SE_Demister), nameof(SE_Demister.UpdateStatusEffect))]
    public static class ModifyWispRange_SE_Demister_UpdateStatusEffect_Patch
    {
        public static void Prefix(SE_Demister __instance, ref bool __state)
        {
            if (!__instance.m_character.IsPlayer())
            {
                __state = false;
                return;
            }

            __state = __instance.m_ballInstance == null;
        }

        public static void Postfix(SE_Demister __instance, ref bool __state)
        {
            if (!__state || __instance.m_ballInstance == null)
            {
                return;
            }

            // First time set up
            var nview = __instance.m_ballInstance.GetComponent<ZNetView>();
            if (nview != null && nview.GetZDO() != null && nview.GetZDO().IsOwner())
            {
                Player player = (Player)__instance.m_character;
                if (player.HasActiveMagicEffect(MagicEffectType.ModifyWispRange, out float effectValue, 0.01f))
                {
                    //EpicLoot.LogErrorForce("Setting Demister range on zdo!");
                    nview.GetZDO().Set(ModifyWispRange.DEMISTER_ZDO, effectValue);

                    var demister = __instance.m_ballInstance.GetComponentInChildren<Demister>();
                    ModifyWispRange.SetDemister(ref demister, effectValue);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Demister), nameof(Demister.Awake))]
    public static class ModifyWispRange_Demister_Awake_Patch
    {
        public static void Postfix(Demister __instance)
        {
            var nview = __instance.transform.root.GetComponent<ZNetView>();

            if (nview == null || nview.GetZDO() == null)
            {
                return;
            }

            float magicEffectValue = nview.GetZDO().GetFloat(ModifyWispRange.DEMISTER_ZDO, 0f);

            ModifyWispRange.SetDemister(ref __instance, magicEffectValue);
        }
    }
}