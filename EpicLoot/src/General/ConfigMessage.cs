using EpicLoot.Config;

namespace EpicLoot;

/// <summary>
/// Main menu panel offering to replace base configs the player has edited with the current defaults.
/// Prefab: Assets/EpicLoot/Prefabs/UI/ConfigMessage. See <see cref="MessagePanelBase"/> for the
/// shared layout contract.
/// </summary>
public sealed class ConfigMessage : MessagePanelBase
{
    public override void OnAcceptClick()
    {
        ConfigVersionManager.BackupAndResetOutdatedConfigs();
        Close();
    }

    public override void OnDenyClick()
    {
        ConfigVersionManager.DeclineOutdatedConfigs();
        Close();
    }
}
