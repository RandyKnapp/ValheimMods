using EpicLoot.Abilities;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    public class UndyingAbility : Ability
    {
        public const string ID = "Undying";

        protected bool _triggered;

        protected override bool ShouldTrigger()
        {
            var shouldTrigger = _triggered;
            _triggered = false;
            return CanActivate() && shouldTrigger;
        }

        protected override void ActivateCustomAction()
        {
            _player.GetSEMan().AddStatusEffect(EpicLoot.LoadAsset<StatusEffect>("UndyingStatusEffect"));
        }

        public void Trigger()
        {
            _triggered = true;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.SetHealth))]
    public class Undying_Character_SetHealth_Patch
    {
        [HarmonyPriority(Priority.Low)]
        private static bool Prefix(Character __instance, float health)
        {
            if (__instance == Player.m_localPlayer && health <= 0)
            {
                var undyingAbility = Player.m_localPlayer.GetAbility<UndyingAbility>(UndyingAbility.ID);
                if (undyingAbility != null && undyingAbility.CanActivate())
                {
                    undyingAbility.Trigger();

                    __instance.Heal(__instance.GetMaxHealth());
                    return false;
                }
            }

            return true;
        }
    }
}
