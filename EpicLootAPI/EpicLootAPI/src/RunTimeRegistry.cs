using System.Collections.Generic;

namespace EpicLootAPI;

/// <summary>
/// Simple registry to hold onto keys returned by EpicLoot,
/// to use to update objects added by specific plugin
/// </summary>
/// <remarks>
/// 1. Add object into EpicLoot
/// 2. EpicLoot returns unique key (string)
/// 3. Save unique key
/// 4. Update object by using unique key
/// </remarks>
internal static class RunTimeRegistry
{
    private static readonly Dictionary<object, string> registry = new();

    /// <param name="key"><see cref="object"/></param>
    /// <param name="value">unique identifier <see cref="string"/></param>
    public static void Register(object key, string value) => registry[key] = value;

    /// <param name="key"><see cref="object"/></param>
    /// <param name="value">unique identifier <see cref="string"/></param>
    /// <returns>true if key found matching object</returns>
    public static bool TryGetValue(object key, out string value) => registry.TryGetValue(key, out value);
}