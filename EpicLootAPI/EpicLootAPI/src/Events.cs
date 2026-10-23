using JetBrains.Annotations;
using System;

namespace EpicLootAPI;

/// <summary>
/// Lifecycle notifications from Epic Loot.
/// </summary>
public static partial class EpicLoot
{
    /// <summary>
    /// Reason tokens handed to a <see cref="AddMagicItemChangedListener"/> callback. String constants
    /// rather than an enum, matching the wire format Epic Loot sends.
    /// </summary>
    [PublicAPI]
    public static class ChangeReason
    {
        public const string Enchant = "Enchant";
        public const string Augment = "Augment";
        public const string Disenchant = "Disenchant";
        public const string Rune = "Rune";
        public const string Temper = "Temper";
        public const string Socket = "Socket";
        public const string Unsocket = "Unsocket";
        public const string LootRoll = "LootRoll";
        public const string Transfer = "Transfer";
        public const string AddSocket = "AddSocket";

        /// <summary>A magic-data write with no dedicated call site.</summary>
        public const string Unspecified = "Unspecified";
    }

    private static readonly Method API_AddMagicItemChangedListener = new(
        "AddMagicItemChangedListener", typeof(Action<ItemDrop.ItemData, string>));

    private static readonly Method API_RemoveMagicItemChangedListener = new(
        "RemoveMagicItemChangedListener", typeof(Action<ItemDrop.ItemData, string>));

    private static readonly Method API_AddLootGeneratedListener = new(
        "AddLootGeneratedListener", typeof(Action<ItemDrop.ItemData>));

    private static readonly Method API_RemoveLootGeneratedListener = new(
        "RemoveLootGeneratedListener", typeof(Action<ItemDrop.ItemData>));

    private static readonly Method API_AddBountyCompletedListener = new(
        "AddBountyCompletedListener", typeof(Action<Player, string>));

    private static readonly Method API_RemoveBountyCompletedListener = new(
        "RemoveBountyCompletedListener", typeof(Action<Player, string>));

    private static readonly Method API_AddAdventureDataChangedListener = new(
        "AddAdventureDataChangedListener", typeof(Action));

    private static readonly Method API_RemoveAdventureDataChangedListener = new(
        "RemoveAdventureDataChangedListener", typeof(Action));

    private static readonly Method API_AddBountyTargetSlainListener = new(
        "AddBountyTargetSlainListener", typeof(Action<string>));

    private static readonly Method API_RemoveBountyTargetSlainListener = new(
        "RemoveBountyTargetSlainListener", typeof(Action<string>));

    /// <summary>
    /// Fires whenever an item's magic data is written -- enchant, augment, disenchant, rune extract,
    /// temper, socket, unsocket, loot roll, or any other write. The new data is already committed when
    /// your callback runs.
    /// </summary>
    /// <param name="listener">Receives the item and a <see cref="ChangeReason"/> token</param>
    /// <returns>true if subscribed</returns>
    [PublicAPI]
    public static bool AddMagicItemChangedListener(Action<ItemDrop.ItemData, string> listener)
    {
        return (bool)(API_AddMagicItemChangedListener.Invoke(listener)[0] ?? false);
    }

    [PublicAPI]
    public static bool RemoveMagicItemChangedListener(Action<ItemDrop.ItemData, string> listener)
    {
        return (bool)(API_RemoveMagicItemChangedListener.Invoke(listener)[0] ?? false);
    }

    /// <summary>
    /// Fires for each magic item Epic Loot rolls as loot, after its magic data is applied.
    /// </summary>
    [PublicAPI]
    public static bool AddLootGeneratedListener(Action<ItemDrop.ItemData> listener)
    {
        return (bool)(API_AddLootGeneratedListener.Invoke(listener)[0] ?? false);
    }

    [PublicAPI]
    public static bool RemoveLootGeneratedListener(Action<ItemDrop.ItemData> listener)
    {
        return (bool)(API_RemoveLootGeneratedListener.Invoke(listener)[0] ?? false);
    }

    /// <summary>
    /// Fires on the claiming player's client when an adventure-mode bounty reward is collected. The
    /// second argument is the bounty's target monster id.
    /// </summary>
    [PublicAPI]
    public static bool AddBountyCompletedListener(Action<Player, string> listener)
    {
        return (bool)(API_AddBountyCompletedListener.Invoke(listener)[0] ?? false);
    }

    [PublicAPI]
    public static bool RemoveBountyCompletedListener(Action<Player, string> listener)
    {
        return (bool)(API_RemoveBountyCompletedListener.Invoke(listener)[0] ?? false);
    }

    /// <summary>
    /// Fires on the local client after the local player's adventure data (bounties, treasure maps) is
    /// written, including the routine save with the character, so compare before acting. Read the new
    /// state with <see cref="GetActiveBounties"/>. Requires API 2.
    /// </summary>
    [PublicAPI]
    public static bool AddAdventureDataChangedListener(Action listener)
    {
        return (bool)(API_AddAdventureDataChangedListener.Invoke(listener)[0] ?? false);
    }

    [PublicAPI]
    public static bool RemoveAdventureDataChangedListener(Action listener)
    {
        return (bool)(API_RemoveAdventureDataChangedListener.Invoke(listener)[0] ?? false);
    }

    /// <summary>
    /// Fires on the server (or a listen host) each time any bounty creature dies, owner online or not,
    /// with JSON <c>{ BountyId, PlayerId, MonsterId, IsAdd }</c>. BountyId matches
    /// <see cref="ActiveBounty.Id"/>. Requires API 2.
    /// </summary>
    [PublicAPI]
    public static bool AddBountyTargetSlainListener(Action<string> listener)
    {
        return (bool)(API_AddBountyTargetSlainListener.Invoke(listener)[0] ?? false);
    }

    [PublicAPI]
    public static bool RemoveBountyTargetSlainListener(Action<string> listener)
    {
        return (bool)(API_RemoveBountyTargetSlainListener.Invoke(listener)[0] ?? false);
    }
}
