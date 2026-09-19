using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Right-stick scrolling for the crafting/enchanting panels.
///
/// This exists as a choke point on purpose. Valheim keeps reshaping ZInput -- the 2026-09-17 patch
/// turned <c>GetJoyRightStickY(bool smooth)</c> into a parameterless method -- and Mono reports an
/// unresolvable callee by failing to JIT the *whole* method that contains the call, before a single
/// line of it runs (the give-away in a log is a MissingMethodException with an empty stack trace).
/// Four panels read the stick straight from their own <c>Update</c>, so one renamed getter killed
/// <c>base.Update()</c> too: the enchant/augment countdown never reached <c>DoMainAction</c> and the
/// craft-success dialog, whose only <c>Close()</c> caller is its <c>Update</c>, could not be
/// dismissed. The enchanting table looked dead for keyboard players as well, because the JIT failure
/// happens regardless of the <c>IsGamepadActive</c> guard around it.
///
/// Routing the read through here keeps that blast radius at "stick scrolling stops working".
/// </summary>
internal static class GamepadScroll
{
    private const float Deadzone = 0.5f;
    private const float Step = -0.1f;

    private static bool _stickReadUnavailable;

    /// <summary>
    /// Nudge <paramref name="scrollbar"/> by the right stick's Y axis. No-op when the scrollbar is
    /// missing, or once the game has been found to no longer expose the getter we compiled against.
    /// </summary>
    public static void ApplyRightStickY(Scrollbar scrollbar)
    {
        if (scrollbar == null || _stickReadUnavailable)
        {
            return;
        }

        float axis;
        try
        {
            axis = ReadRightStickY();
        }
        catch (Exception e)
        {
            // MissingMethodException/TypeLoadException: the game changed ZInput's signature out from
            // under this build. Latch it off so this costs one failed JIT, not one per frame, and say
            // so unconditionally -- it means the mod needs a rebuild against the current game.
            _stickReadUnavailable = true;
            EpicLoot.LogWarningForce("Gamepad stick scrolling disabled: ZInput.GetJoyRightStickY() " +
                $"could not be called ({e.GetType().Name}: {e.Message}). EpicLoot needs a rebuild " +
                "against the current Valheim version.");
            return;
        }

        if (Mathf.Abs(axis) > Deadzone)
        {
            scrollbar.value = Mathf.Clamp01(scrollbar.value + axis * Step);
        }
    }

    // NoInlining is load-bearing: inlining this into ApplyRightStickY would move the unresolvable
    // call site there and the JIT failure would bypass the try/catch above.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ReadRightStickY()
    {
        return ZInput.GetJoyRightStickY();
    }
}
