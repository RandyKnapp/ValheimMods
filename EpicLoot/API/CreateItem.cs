using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace EpicLoot;

/// <summary>
/// Creating items that carry chosen magic: a reward with set effects, a quest item, a runestone with one
/// specific effect. <see cref="TryMakeMagicItem"/> rolls; this writes exactly what was asked.
/// </summary>
public static partial class API
{
    /// <summary>
    /// Creates a new item from a prefab, or an etched runestone of a rarity, carrying exactly the effects,
    /// unique and set the request names. Nothing is checked against the rules a roll follows: unknown effects,
    /// effects the item could never roll and items that never roll magic are all written as asked. Only a
    /// request that cannot be built is refused (an unknown prefab or rarity, a non-finite value).
    /// </summary>
    /// <param name="requestJson">JSON <c>{ Prefab, Runestone, Rarity, Effects: [{ EffectType, EffectValue }],
    /// LegendaryID, SetID, DisplayName, SocketCount }</c>; see docs/API.md for what each field does.</param>
    /// <param name="error">Receives why nothing was created; unchanged on success.</param>
    /// <returns>The new item, in no inventory yet, or null</returns>
    [PublicAPI]
    public static ItemDrop.ItemData CreateMagicItem(string requestJson, ref string error)
    {
        if (string.IsNullOrEmpty(requestJson))
        {
            error = "empty request";
            return null;
        }

        MagicItemRequest request;
        try
        {
            request = JsonConvert.DeserializeObject<MagicItemRequest>(requestJson);
        }
        catch (Exception ex)
        {
            error = $"could not parse the request: {ex.Message}";
            return null;
        }

        try
        {
            ItemDrop.ItemData item = MagicItemFactory.Create(request, out string reason);
            if (item == null)
            {
                error = reason;
            }

            return item;
        }
        catch (Exception ex)
        {
            EpicLoot.LogErrorForce($"[EpicLoot.API] CreateMagicItem failed for {request?.Prefab}: {ex}");
            error = ex.Message;
            return null;
        }
    }
}
