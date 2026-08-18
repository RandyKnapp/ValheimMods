using EpicLoot.Abilities;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void ResetAbilityCooldowns(Terminal.ConsoleEventArgs args)
    {
        Player player = Player.m_localPlayer;
        if (player != null)
        {
            if (player.TryGetComponent(out AbilityController abilityController))
            {
                foreach (Ability ability in abilityController.CurrentAbilities)
                {
                    ability.ResetCooldown();
                }
                args.Context.PrintInfo("> Abilities reset");
            }
            else
            {
                args.Context.PrintWarning("> Local player ability controller is not available");
            }
        }
        else
        {
            args.Context.PrintWarning("> Local player is not available");
        }
    }
}