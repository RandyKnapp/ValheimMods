using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace EpicLoot;

/// <summary>
/// Lifecycle notifications. Exposed as explicit Add/Remove listener calls rather than public
/// <c>event</c> fields so a reflection-only consumer can subscribe with a plain method call.
///
/// Listeners run inside a try/catch; one that throws is logged with the exception and does not stop the
/// remaining listeners or the game action that raised it.
/// </summary>
public static partial class API
{
    /// <summary>
    /// Reason tokens passed to a magic-item-changed listener. String constants rather than an enum, so
    /// they cross the API boundary without a shared type.
    /// </summary>
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

        /// <summary>
        /// A write that has no dedicated call site. Every magic-data write funnels through
        /// <c>MagicItemComponent.SetMagicItem</c>, so this is the catch-all that guarantees coverage.
        /// </summary>
        public const string Unspecified = "Unspecified";
    }

    private static event Action<ItemDrop.ItemData, string> MagicItemChanged;
    private static event Action<ItemDrop.ItemData> LootGenerated;
    private static event Action<Player, string> BountyCompleted;
    private static event Action AdventureDataChanged;
    private static event Action<string> BountyTargetSlain;

    // Set while a semantic raise is in flight. SetMagicItem sits underneath every crafting operation, so
    // without this a single enchant would report both "Enchant" and "Unspecified".
    private static string _activeChangeReason;

    // Item load is not a change: a MagicItemComponent that corrects stored data while loading, or a vanilla
    // item given the magic it is born with, writes through SetMagicItem on items entering the world, which
    // would otherwise flood listeners at world load.
    internal static bool SuppressChangeEvents;

    private static void LogListenerFailure(string family, Exception ex)
    {
        // Force-logged: a listener that throws belongs to another plugin, and swallowing it silently
        // makes the resulting missed-notification report impossible to diagnose.
        EpicLoot.LogErrorForce($"[EpicLoot.API] {family} listener threw: {ex}");
    }

    /// <summary>
    /// Fires whenever an item's magic data is written -- enchant, augment, disenchant, rune extract,
    /// temper, socket, unsocket, loot roll, or any other write.
    /// </summary>
    /// <param name="listener">Receives the item and a <see cref="ChangeReason"/> token. The item's new
    /// magic data is already committed when this runs.</param>
    /// <returns>true if subscribed</returns>
    [PublicAPI]
    public static bool AddMagicItemChangedListener(Action<ItemDrop.ItemData, string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        MagicItemChanged += listener;
        return true;
    }

    /// <returns>true if the listener was non-null; unsubscribing something never subscribed is a no-op</returns>
    [PublicAPI]
    public static bool RemoveMagicItemChangedListener(Action<ItemDrop.ItemData, string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        MagicItemChanged -= listener;
        return true;
    }

    /// <summary>
    /// Fires for each magic item Epic Loot rolls as loot, after its magic data is applied and before it
    /// is handed to the world.
    /// </summary>
    [PublicAPI]
    public static bool AddLootGeneratedListener(Action<ItemDrop.ItemData> listener)
    {
        if (listener == null)
        {
            return false;
        }

        LootGenerated += listener;
        return true;
    }

    /// <returns>true if the listener was non-null</returns>
    [PublicAPI]
    public static bool RemoveLootGeneratedListener(Action<ItemDrop.ItemData> listener)
    {
        if (listener == null)
        {
            return false;
        }

        LootGenerated -= listener;
        return true;
    }

    /// <summary>
    /// Fires on the claiming player's client when an adventure-mode bounty reward is collected.
    /// </summary>
    /// <param name="listener">Receives the player and the bounty's target monster id</param>
    [PublicAPI]
    public static bool AddBountyCompletedListener(Action<Player, string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        BountyCompleted += listener;
        return true;
    }

    /// <returns>true if the listener was non-null</returns>
    [PublicAPI]
    public static bool RemoveBountyCompletedListener(Action<Player, string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        BountyCompleted -= listener;
        return true;
    }

    /// <summary>
    /// Fires on the local client after the local player's adventure data (bounties, treasure maps) is
    /// written: a bounty accepted, a target or add slain, a bounty completed, claimed or abandoned, a map
    /// bought or its chest found, offline kills replayed, and the routine save with the character.
    /// </summary>
    /// <remarks>
    /// Carries no payload; read what you need, e.g. <see cref="GetActiveBounties"/>. It can fire when
    /// nothing you care about changed (the routine save), so compare before acting on it.
    /// </remarks>
    [PublicAPI]
    public static bool AddAdventureDataChangedListener(Action listener)
    {
        if (listener == null)
        {
            return false;
        }

        AdventureDataChanged += listener;
        return true;
    }

    /// <returns>true if the listener was non-null</returns>
    [PublicAPI]
    public static bool RemoveAdventureDataChangedListener(Action listener)
    {
        if (listener == null)
        {
            return false;
        }

        AdventureDataChanged -= listener;
        return true;
    }

    /// <summary>
    /// Fires on the server (a dedicated server or a listen host) each time any bounty creature dies,
    /// whether or not the bounty's owner is online. Receives JSON
    /// <c>{ BountyId, PlayerId, MonsterId, IsAdd }</c>; BountyId matches <see cref="GetActiveBounties"/>'s Id
    /// and PlayerId is the owner.
    /// </summary>
    /// <remarks>
    /// Raised for every kill the server relays, including kills of an abandoned bounty's leftover
    /// creatures, which the owner no longer gets credit for.
    /// </remarks>
    [PublicAPI]
    public static bool AddBountyTargetSlainListener(Action<string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        BountyTargetSlain += listener;
        return true;
    }

    /// <returns>true if the listener was non-null</returns>
    [PublicAPI]
    public static bool RemoveBountyTargetSlainListener(Action<string> listener)
    {
        if (listener == null)
        {
            return false;
        }

        BountyTargetSlain -= listener;
        return true;
    }

    /// <summary>
    /// Labels the write performed by <paramref name="write"/>, so the event <c>SetMagicItem</c> raises
    /// carries a real reason token instead of <see cref="ChangeReason.Unspecified"/>. The event still
    /// fires exactly once, from the funnel.
    /// </summary>
    /// <remarks>
    /// Nested calls keep the outermost reason: socketing re-hosts effects through several writes, and
    /// the caller's intent is the interesting one.
    /// </remarks>
    internal static void WithChangeReason(string reason, Action write)
    {
        if (_activeChangeReason != null)
        {
            write();
            return;
        }

        _activeChangeReason = reason;
        try
        {
            write();
        }
        finally
        {
            _activeChangeReason = null;
        }
    }

    /// <summary>
    /// Called from <c>MagicItemComponent.SetMagicItem</c>, the single funnel every magic-data write
    /// passes through. Picks up the reason from an enclosing <see cref="WithChangeReason"/>, if any.
    /// </summary>
    internal static void RaiseMagicItemChanged(ItemDrop.ItemData item)
    {
        RaiseMagicItemChanged(item, _activeChangeReason ?? ChangeReason.Unspecified);
    }

    /// <summary>
    /// Direct raise for operations that do not write through <c>SetMagicItem</c> -- disenchanting and a
    /// fully-extracted rune both drop the component outright rather than writing to it.
    /// </summary>
    internal static void RaiseMagicItemChanged(ItemDrop.ItemData item, string reason)
    {
        if (SuppressChangeEvents || item == null || MagicItemChanged == null)
        {
            return;
        }

        foreach (Delegate handler in MagicItemChanged.GetInvocationList())
        {
            try
            {
                ((Action<ItemDrop.ItemData, string>)handler)(item, reason);
            }
            catch (Exception ex)
            {
                LogListenerFailure("MagicItemChanged", ex);
            }
        }
    }

    internal static void RaiseLootGenerated(ItemDrop.ItemData item)
    {
        if (item == null || LootGenerated == null)
        {
            return;
        }

        foreach (Delegate handler in LootGenerated.GetInvocationList())
        {
            try
            {
                ((Action<ItemDrop.ItemData>)handler)(item);
            }
            catch (Exception ex)
            {
                LogListenerFailure("LootGenerated", ex);
            }
        }
    }

    internal static void RaiseBountyCompleted(Player player, string targetId)
    {
        if (BountyCompleted == null)
        {
            return;
        }

        foreach (Delegate handler in BountyCompleted.GetInvocationList())
        {
            try
            {
                ((Action<Player, string>)handler)(player, targetId);
            }
            catch (Exception ex)
            {
                LogListenerFailure("BountyCompleted", ex);
            }
        }
    }

    /// <summary>
    /// Called from <c>AdventureComponent.Save</c>, which every adventure state change passes through.
    /// </summary>
    internal static void RaiseAdventureDataChanged()
    {
        if (AdventureDataChanged == null)
        {
            return;
        }

        foreach (Delegate handler in AdventureDataChanged.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                LogListenerFailure("AdventureDataChanged", ex);
            }
        }
    }

    /// <summary>
    /// Called from the server's <c>SlayBountyTarget</c> handler.
    /// </summary>
    internal static void RaiseBountyTargetSlain(Adventure.BountyInfo bounty, string monsterID, bool isAdd)
    {
        if (BountyTargetSlain == null || bounty?.Target == null)
        {
            return;
        }

        string json = JsonConvert.SerializeObject(new
        {
            BountyId = bounty.ID,
            PlayerId = bounty.PlayerID,
            MonsterId = monsterID,
            IsAdd = isAdd
        });

        foreach (Delegate handler in BountyTargetSlain.GetInvocationList())
        {
            try
            {
                ((Action<string>)handler)(json);
            }
            catch (Exception ex)
            {
                LogListenerFailure("BountyTargetSlain", ex);
            }
        }
    }
}
