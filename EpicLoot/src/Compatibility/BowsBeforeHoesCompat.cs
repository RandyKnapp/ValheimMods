using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using EpicLoot.ShardStones;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.Compatibility;

/// <summary>
/// Lets BowsBeforeHoes' quivers and Epic Loot's shard slots share the inventory. BowsBeforeHoes 2.x opens a
/// quiver on Use from its own <c>InventoryGui.Update</c> prefix, ordered ahead of Epic Loot's, and stands aside
/// only while a key it looks up in Epic Loot's internals by name is held; it also draws a quiver's whole
/// inventory tooltip itself unless it finds an Epic Loot tooltip method to hook. Neither name exists any more.
/// Rather than keep them alive for it, this patches BowsBeforeHoes:
/// <list type="bullet">
/// <item>its quiver opener sits out the frames where the press is Epic Loot's shard slot gesture over an item
/// with shard slots, so Epic Loot's opener gets that press and every other press still opens the quiver;</item>
/// <item>its quiver tooltip stands aside for Epic Loot's, which shows the quiver's open prompt as a hint;</item>
/// <item>its public <c>API.IsQuiver</c> tells <see cref="ForeignItemContainers"/> which items are quivers.</item>
/// </list>
/// Every target is looked up by name in BowsBeforeHoes' own assembly and patched on its own. One that is missing
/// is skipped (1.x has none of them: its quivers are Backpacks containers, which ForeignItemContainers already
/// knows), and nothing here throws, so a BowsBeforeHoes update degrades to the behaviour without this module.
/// </summary>
public static class BowsBeforeHoesCompat
{
    public const string PluginGuid = "Azumatt.BowsBeforeHoes";

    private const string ApiType = "BowsBeforeHoes.API";
    private const string QuiverGuiType = "BowsBeforeHoes.QuiverFunctionality.QuiverContainerGui";

    private const BindingFlags AnyStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.DeclaredOnly;

    private static Func<ItemDrop.ItemData, bool> _isQuiver;
    private static Func<string> _openPrompt;

    /// <summary>
    /// Patches BowsBeforeHoes when it is installed. Call once from Awake: Epic Loot soft-depends on it, so its
    /// assembly is loaded by then. Its own patches may land before or after these; either order works, since
    /// these patch its methods rather than the game's.
    /// </summary>
    public static void Initialize(Harmony harmony)
    {
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo plugin) || plugin.Instance == null)
        {
            return;
        }

        string version = plugin.Metadata.Version.ToString();
        try
        {
            Assembly assembly = plugin.Instance.GetType().Assembly;
            _isQuiver = Bind<Func<ItemDrop.ItemData, bool>>(assembly, ApiType, "IsQuiver");
            bool opener = Patch(harmony, assembly, QuiverGuiType + "+OpenFromUseKey", "Prefix", nameof(OpenerPrefix));
            bool tooltip = Patch(harmony, assembly, QuiverGuiType + "+ShowOpenPrompt", "Prefix", nameof(TooltipPrefix));

            // Only shown while BowsBeforeHoes' own tooltip stands aside; otherwise it already shows this line.
            if (tooltip)
            {
                _openPrompt = Bind<Func<string>>(assembly, QuiverGuiType, "OpenPrompt");
            }

            if (_isQuiver == null && !opener && !tooltip)
            {
                EpicLoot.Log($"BowsBeforeHoes {version} found; it opens no quivers of its own, so there is nothing to patch.");
            }
            else if (_isQuiver != null && opener && tooltip && _openPrompt != null)
            {
                EpicLoot.Log($"BowsBeforeHoes {version} found; its quiver opener and tooltip now make way for shard slots.");
            }
            else
            {
                EpicLoot.LogWarningForce($"BowsBeforeHoes {version} found, but only part of it could be patched " +
                    $"(quiver check: {Found(_isQuiver != null)}, quiver opener: {Found(opener)}, " +
                    $"quiver tooltip: {Found(tooltip && _openPrompt != null)}). Shard slots on its quivers may not open.");
            }
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not patch BowsBeforeHoes {version}: {e.Message}");
        }
    }

    /// <summary>True for a BowsBeforeHoes 2.x quiver. Never throws: a failing check is dropped.</summary>
    public static bool IsQuiver(ItemDrop.ItemData item)
    {
        if (_isQuiver == null || item == null)
        {
            return false;
        }

        try
        {
            return _isQuiver(item);
        }
        catch (Exception e)
        {
            _isQuiver = null;
            EpicLoot.LogWarningForce($"BowsBeforeHoes' quiver check threw and is no longer used: {e.Message}");
            return false;
        }
    }

    /// <summary>BowsBeforeHoes' own "open the quiver" line for a quiver, or null.</summary>
    public static string GetOpenPrompt(ItemDrop.ItemData item)
    {
        if (_openPrompt == null || !IsQuiver(item))
        {
            return null;
        }

        try
        {
            return _openPrompt();
        }
        catch (Exception e)
        {
            _openPrompt = null;
            EpicLoot.LogWarningForce($"BowsBeforeHoes' quiver prompt threw and is no longer shown: {e.Message}");
            return null;
        }
    }

    // Takes no arguments on purpose: the target's own parameter is named __instance, which Harmony would read
    // as the (null) instance of a static method rather than as that argument. A confirmation over the
    // inventory owns every press until it is answered, the quiver's included.
    [UsedImplicitly]
    private static bool OpenerPrefix()
    {
        if (!InventoryGui.IsVisible())
        {
            return true;
        }

        return !InventoryPromptHost.IsOpen && !SocketOverlayInput.IsShardSlotPress(InventoryGui.instance);
    }

    // Skipping BowsBeforeHoes' prefix with __result true lets InventoryGrid.CreateItemTooltip carry on, so Epic
    // Loot's own prefix draws the quiver. While another mod has taken the inventory tooltip from Epic Loot,
    // BowsBeforeHoes' tooltip is the only one that names the quiver's key, so it is left to run.
    [UsedImplicitly]
    private static bool TooltipPrefix(ref bool __result)
    {
        if (ItemTooltip.IsOverridden)
        {
            return true;
        }

        __result = true;
        return false;
    }

    private static string Found(bool found) => found ? "patched" : "not found";

    private static MethodInfo FindMethod(Assembly assembly, string typeName, string methodName)
    {
        // Plain reflection rather than AccessTools, which logs a warning for every name it cannot find.
        return assembly.GetType(typeName, throwOnError: false)?.GetMethod(methodName, AnyStatic);
    }

    private static T Bind<T>(Assembly assembly, string typeName, string methodName) where T : Delegate
    {
        MethodInfo method = FindMethod(assembly, typeName, methodName);
        return method == null ? null : Delegate.CreateDelegate(typeof(T), method, throwOnBindFailure: false) as T;
    }

    private static bool Patch(Harmony harmony, Assembly assembly, string typeName, string methodName, string prefix)
    {
        MethodInfo target = FindMethod(assembly, typeName, methodName);
        if (target == null)
        {
            return false;
        }

        try
        {
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(BowsBeforeHoesCompat), prefix));
            return true;
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not patch BowsBeforeHoes' {typeName}.{methodName}: {e.Message}");
            return false;
        }
    }
}
