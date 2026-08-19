using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EpicLoot;

public static partial class API
{
    /// <summary>
    /// The version of the public API contract, independent of <see cref="EpicLoot.Version"/>.
    /// Bump this on every additive change; a consumer compares it to decide which endpoints exist.
    /// </summary>
    /// <remarks>
    /// 1 - Queries, providers (inventory / equipment / sacrifice), lifecycle events, loot generation,
    ///     item slot and icon decoration.
    /// </remarks>
    [PublicAPI]
    public const int ApiVersion = 1;

    /// <returns><see cref="ApiVersion"/>, as a call so reflection consumers can read it without
    /// resolving a const field (which would be inlined at their compile time, not ours).</returns>
    [PublicAPI]
    public static int GetApiVersion()
    {
        return ApiVersion;
    }

    /// <returns>The running Epic Loot plugin version, e.g. "0.13.0".</returns>
    [PublicAPI]
    public static string GetPluginVersion()
    {
        return EpicLoot.Version;
    }

    /// <returns>The Epic Loot plugin GUID, for BepInEx soft-dependency checks.</returns>
    [PublicAPI]
    public static string GetPluginId()
    {
        return EpicLoot.PluginId;
    }

    /// <summary>
    /// Feature probe. Lets a plugin built against a newer API degrade gracefully on an older Epic Loot
    /// instead of throwing when a method it expects is missing.
    /// </summary>
    /// <param name="name">The exact, case-sensitive name of a public static method on this class.</param>
    /// <returns>true if at least one overload by that name exists</returns>
    [PublicAPI]
    public static bool HasEndpoint(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return EndpointNames.Contains(name);
    }

    /// <returns>Every public endpoint name on this class, for diagnostics.</returns>
    [PublicAPI]
    public static List<string> GetEndpointNames()
    {
        return EndpointNames.ToList();
    }

    private static readonly HashSet<string> EndpointNames = typeof(API)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Select(m => m.Name)
        .ToHashSet();

    /// <summary>
    /// Turns on the API's internal registration/reload chatter. Off by default because a healthy
    /// registration is not interesting; failures are logged unconditionally regardless of this flag.
    /// </summary>
    [PublicAPI]
    public static void SetVerboseLogging(bool enabled)
    {
        ShowLogs = enabled;
    }
}
