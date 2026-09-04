using System;
using System.Collections.Generic;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace EpicLoot.Compatibility;

/// <summary>
/// Detects items that another mod has turned into a container -- a backpack, a quiver, a pouch --
/// using the Backpacks <c>ItemContainer</c> pattern. Those items already own the "Use" press while the
/// inventory is open (the container's own hint reads "Press [Use] to open"), which is the same gesture
/// the ShardStone socket overlay uses, so EpicLoot has to know to stand aside.
///
/// Soft in every direction: no assembly reference, no BepInDependency, and any failure degrades to
/// "not a container", which restores the pre-existing behaviour rather than taking anything down.
/// </summary>
public static class ForeignItemContainers
{
    // Every ItemContainer namespaces its custom data under the Backpacks GUID, whichever mod actually
    // declares the subclass -- BowsBeforeHoes, for one, explicitly forces its bundled ItemDataManager
    // copy's _modGuid to this value so its quiver shares that space.
    private const string BackpacksGuid = "org.bepinex.plugins.backpacks";
    private const string KeyPrefix = BackpacksGuid + "#";
    private const char CustomDataSeparator = '#';
    private const string ContainerTypeName = "Backpacks.ItemContainer";

    // ItemDataManager custom-data keys are "<modGuid>#<Type.FullName>[,<AssemblyName>][#<subKey>]" --
    // the assembly is appended only when the type lives outside the assembly that owns that
    // ItemDataManager copy, and the sub-key only when the data was added under a named slot. Either way
    // the class part identifies a type whose ancestry we can test once and remember for the session.
    private static readonly Dictionary<string, bool> ContainerKeys = new Dictionary<string, bool>();

    private static bool _resolvedBackpacks;
    private static bool _backpacksPresent;

    private static bool BackpacksPresent
    {
        get
        {
            if (!_resolvedBackpacks)
            {
                _resolvedBackpacks = true;
                _backpacksPresent = Chainloader.PluginInfos.ContainsKey(BackpacksGuid);
            }

            return _backpacksPresent;
        }
    }

    /// <summary>
    /// True when another mod has attached a container inventory to this item, and therefore owns the
    /// inventory "Use" gesture over it.
    /// </summary>
    public static bool HasContainer(ItemDrop.ItemData item)
    {
        // Free when Backpacks is not installed, which is the overwhelmingly common case.
        if (!BackpacksPresent || item?.m_customData == null || item.m_customData.Count == 0)
        {
            return false;
        }

        try
        {
            foreach (KeyValuePair<string, string> entry in item.m_customData)
            {
                if (entry.Key == null || !entry.Key.StartsWith(KeyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsContainerKey(entry.Key))
                {
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            // Their internals moved, or the item is mid-load. Behave as though there is no container.
            EpicLoot.LogWarning($"Could not inspect foreign container data ({e.Message}); " +
                "treating the item as having no container.");
            return false;
        }

        return false;
    }

    // The custom data is read directly rather than through ItemExtensions.Data(item, mod) /
    // ForeignItemInfo on purpose: that bridge only enumerates data the owning mod has already
    // materialised, so it can report nothing depending on load order. The raw keys are always there.
    private static bool IsContainerKey(string customDataKey)
    {
        if (ContainerKeys.TryGetValue(customDataKey, out bool known))
        {
            return known;
        }

        bool isContainer = false;

        // "<guid>#Some.Namespace.Type,SomeAssembly#slot" -> "Some.Namespace.Type,SomeAssembly"
        string typeName = customDataKey.Substring(KeyPrefix.Length);
        int subKey = typeName.IndexOf(CustomDataSeparator);
        if (subKey >= 0)
        {
            typeName = typeName.Substring(0, subKey);
        }

        // TypeByName takes the bare name too, falling back to a scan of the loaded assemblies -- which
        // is what a container declared inside Backpacks itself produces, since ItemDataManager drops the
        // assembly suffix for its own types.
        Type type = typeName.Length == 0 ? null : AccessTools.TypeByName(typeName);

        for (Type ancestor = type; ancestor != null; ancestor = ancestor.BaseType)
        {
            if (ancestor.FullName == ContainerTypeName)
            {
                isContainer = true;
                break;
            }
        }

        ContainerKeys[customDataKey] = isContainer;
        return isContainer;
    }
}
