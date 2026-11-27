using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using EpicLoot.Transformation;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.Compatibility;

/// <summary>
/// Keeps Dual Wield away from a player in Hexen form. Her attacks are the witch's, but the hand items Dual Wield
/// reads stay equipped under the form, so two of its patches act on them:
/// <list type="bullet">
/// <item>its <c>ZSyncAnimation.RPC_SetTrigger</c> prefix puts a copy of the player's animator controller on
/// <c>Character.m_animator</c> for nearly any trigger while something is in hand. Under the form that is the
/// witch's animator, so her rig played the player's states (seen as sitting down), her attack triggers went
/// nowhere and movement broke until the form was rebuilt;</item>
/// <item>its <c>Attack.OnAttackTrigger</c> prefix, with a one-handed weapon in each hand, adds a left-hand melee
/// swing and leaves the right-hand weapon as the attack's weapon, so a blast dealt that weapon's damage.</item>
/// </list>
/// Both are found by name on Dual Wield's plugin class and skipped for a transformed player. One that is missing is
/// left alone and logged, so a Dual Wield update degrades to the behaviour without this module.
/// </summary>
public static class DualWieldCompat
{
    public const string PluginGuid = "org.bepinex.plugins.dualwield";

    /// <summary>Patches Dual Wield when it is installed. Call once from Awake, after Epic Loot's soft dependency on it.</summary>
    public static void Initialize(Harmony harmony)
    {
        if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out PluginInfo plugin) || plugin.Instance == null)
        {
            return;
        }

        string version = plugin.Metadata.Version.ToString();
        Type pluginType = plugin.Instance.GetType();
        bool trigger = Patch(harmony, pluginType, "Patch_ZSyncAnimation_RPC_SetTrigger", nameof(TriggerPrefix));
        bool attack = Patch(harmony, pluginType, "Patch_Attack_OnAttackTrigger", nameof(AttackTriggerPrefix));

        if (trigger && attack)
        {
            EpicLoot.Log($"Dual Wield {version} found; its animation and attack patches now leave the Hexen form alone.");
        }
        else
        {
            EpicLoot.LogWarningForce($"Dual Wield {version} found, but only part of it could be patched " +
                $"(animation swap: {Found(trigger)}, dual attack: {Found(attack)}). Attacking in Hexen form may break.");
        }
    }

    // The targets are static and name their first parameter __instance, which Harmony would read as their (null)
    // instance, so the argument is taken by position.
    [UsedImplicitly]
    private static bool TriggerPrefix(ZSyncAnimation __0)
    {
        return __0 == null || !HexenForm.TryGet(__0.GetComponent<Character>(), out _);
    }

    [UsedImplicitly]
    private static bool AttackTriggerPrefix(Attack __0)
    {
        return __0 == null || !HexenForm.TryGet(__0.m_character, out _);
    }

    private static string Found(bool found) => found ? "patched" : "not found";

    private static bool Patch(Harmony harmony, Type pluginType, string nestedType, string prefix)
    {
        MethodInfo target = pluginType.GetNestedType(nestedType, BindingFlags.NonPublic)
            ?.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        if (target == null)
        {
            return false;
        }

        try
        {
            harmony.Patch(target, prefix: new HarmonyMethod(typeof(DualWieldCompat), prefix));
            return true;
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not patch Dual Wield's {nestedType}.Prefix: {e.Message}");
            return false;
        }
    }
}
