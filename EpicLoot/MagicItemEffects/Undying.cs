using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
    public class Undying_Character_SetHealth_Patch
    {
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(Character __instance, float health)
        {
            if (__instance == Player.m_localPlayer && health <= 0)
            {
                if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.Undying))
                {
                    // TODO: Add cooldown to undying here
                    __instance.Heal(__instance.GetMaxHealth());
                    __instance.GetComponent<ZNetView>().InvokeRPC(ZRoutedRpc.Everybody,"epic loot undying display");
                    __instance.GetSEMan().AddStatusEffect("Bulwark", true);
                    return false;
                }
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    public class Undying_Player_Awake_Patch
    {
        private static void Postfix(Player __instance)
        {
            __instance.GetComponent<ZNetView>().Register("epic loot undying display", s => RPC_Undying(__instance));
        }

        private static void RPC_Undying(Player player)
        {
            // TODO: Add visual effect for undying here
            var protectPrefab = ZNetScene.instance.GetPrefab("fx_shaman_protect");
            var protect = Object.Instantiate(protectPrefab, player.transform.localPosition, Quaternion.identity);
            protect.GetComponent<TimedDestruction>().m_timeout = 4f;
            protect.name = "undying effect";
            protect.transform.SetParent(player.transform);
            var particles = protect.transform.Find("rising particles");
            particles.GetComponent<Renderer>().material.color = new Color(0.6f, 0.6f, 1f, 1f);
            var main = particles.GetComponent<ParticleSystem>().main;
            main.simulationSpeed = 2f;

            protect.transform.Find("delayed_shockwave").gameObject.SetActive(false);
        }
    }
}
