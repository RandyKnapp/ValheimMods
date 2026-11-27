using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Right-stick scrolling for the crafting/enchanting panels, read through one choke point on purpose.
/// Valheim keeps reshaping ZInput, and Mono reports an unresolvable callee by failing to JIT the
/// *whole* method that contains the call, before a single line runs (a MissingMethodException with an
/// empty stack trace), whatever <c>IsGamepadActive</c> guard surrounds it. Read straight from a panel's
/// <c>Update</c>, one renamed getter stops that whole Update (and <c>base.Update()</c>) for keyboard
/// players too; read here, it only stops stick scrolling.
/// </summary>
internal static class GamepadScroll
{
    private const float Deadzone = 0.5f;
    private const float Step = -0.1f;
    private const float PixelsPerSecond = 1000f;

    private static bool _stickReadUnavailable;

    /// <summary>
    /// Nudge <paramref name="scrollbar"/> by the right stick's Y axis. No-op when the scrollbar is
    /// missing, or once the game has been found to no longer expose the getter we compiled against.
    /// </summary>
    public static void ApplyRightStickY(Scrollbar scrollbar)
    {
        if (scrollbar == null || !TryReadRightStickY(out float axis))
        {
            return;
        }

        scrollbar.value = Mathf.Clamp01(scrollbar.value + axis * Step);
    }

    public static void ApplyRightStickY(ScrollRect scrollRect)
    {
        if (scrollRect == null || scrollRect.content == null)
        {
            return;
        }

        RectTransform viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        float scrollable = scrollRect.content.rect.height - viewport.rect.height;
        if (scrollable <= 0f || !TryReadRightStickY(out float axis))
        {
            return;
        }

        float pixels = -axis * PixelsPerSecond * Time.unscaledDeltaTime;
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + pixels / scrollable);
    }

    private static bool TryReadRightStickY(out float axis)
    {
        axis = 0f;
        if (_stickReadUnavailable)
        {
            return false;
        }

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
            return false;
        }

        return Mathf.Abs(axis) > Deadzone;
    }

    // NoInlining is load-bearing: inlining this into TryReadRightStickY would move the unresolvable
    // call site there and the JIT failure would bypass the try/catch above.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ReadRightStickY()
    {
        return ZInput.GetJoyRightStickY();
    }
}
