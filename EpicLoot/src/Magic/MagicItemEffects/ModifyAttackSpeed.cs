using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    public static class ModifyAttackSpeed_ApplyAnimationHandler_Patch
    {
        internal static bool appliedAttackSpeed = false;
        public static double ModifyAttackSpeed(Character character, double speed)
        {
            if (character is Player player && player.InAttack() && player.m_currentAttack != null)
            {
                speed *= 1.0d + player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyAttackSpeed, 0.01f);
                if (ModifyWithLowHealth.PlayerHasLowHealth(player))
                {
                    speed *= 1.0d + player.GetTotalActiveMagicEffectValue(
                        ModifyWithLowHealth.EffectNameWithLowHealth(MagicEffectType.ModifyAttackSpeed), 0.01f);
                }
            }

            return speed;
        }

        [UsedImplicitly]
        private static void Postfix(Game __instance)
        {
            if (appliedAttackSpeed == false)
            {
                AnimationSpeedManager.Add((character, speed) => ModifyAttackSpeed(character, speed));
                appliedAttackSpeed = true;
            }
            
        }
    }
}
