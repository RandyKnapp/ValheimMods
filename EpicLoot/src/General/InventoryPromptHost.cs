using System;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// The single owner of "a modal yes/no prompt is up over the inventory window".
///
/// Two things make this shared rather than per-feature. The prompt has to be driven from a Harmony
/// prefix on InventoryGui.Update -- a MonoBehaviour Update may run either side of it, and if it
/// answered first, vanilla would Hide() the inventory in the same frame; a prefix always runs before
/// the original, so the ordering is certain. And whichever prompt is up has to swallow every press
/// that vanilla would otherwise turn into a Hide(), which means exactly one of them may be open at a
/// time. Both features route through here so they cannot fight each other for the Use press.
/// </summary>
public static class InventoryPromptHost
{
    private static ConfirmPrompt _active;

    public static bool IsOpen
    {
        get
        {
            // Unity's null operator reports a destroyed panel as null (the inventory being torn down,
            // for instance); drop the stale reference rather than holding the modal open forever.
            if (_active == null)
            {
                _active = null;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Takes ownership of an already-created prompt and wires its callbacks. Returns false when one is
    /// already open, in which case the new panel is destroyed and nothing is shown -- callers should
    /// have checked <see cref="IsOpen"/> first. A null prompt (its prefab was missing from the bundle)
    /// is also refused, so a caller that fails closed on that keeps working.
    /// </summary>
    public static bool Open(ConfirmPrompt prompt, Action onAccept, Action onDeny = null)
    {
        if (prompt == null)
        {
            return false;
        }

        if (IsOpen)
        {
            prompt.Close();
            return false;
        }

        _active = prompt;
        // Clear the host's reference before running the caller's action: the action may open the next
        // prompt, and it must not be refused by the one that is on its way out.
        prompt.OnAccept = () =>
        {
            _active = null;
            onAccept?.Invoke();
        };
        prompt.OnDeny = () =>
        {
            _active = null;
            onDeny?.Invoke();
        };

        return true;
    }

    /// <summary>
    /// Drives the open prompt and swallows every press that would otherwise act on the window behind
    /// it. Gamepad A accepts and B denies -- the panel's buttons cannot be clicked without a mouse, so
    /// A is the only way to confirm there. Call from the InventoryGui.Update prefix, before anything
    /// else reads input.
    /// </summary>
    public static void Update()
    {
        if (!IsOpen)
        {
            return;
        }

        var accept = ZInput.GetButtonDown("JoyButtonA");
        var deny = ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape);

        // Consume everything vanilla InventoryGui.Update would otherwise turn into a Hide().
        ZInput.ResetButtonStatus("Use");
        ZInput.ResetButtonStatus("JoyUse");
        ZInput.ResetButtonStatus("JoyButtonA");
        ZInput.ResetButtonStatus("JoyButtonB");
        ZInput.ResetButtonStatus("JoyButtonY");
        ZInput.ResetButtonStatus("Inventory");

        var prompt = _active;
        if (accept)
        {
            prompt.OnAcceptClick();
        }
        else if (deny)
        {
            prompt.OnDenyClick();
        }
    }

    /// <summary>Drops an unanswered prompt without running either callback.</summary>
    public static void Cancel()
    {
        if (!IsOpen)
        {
            return;
        }

        var prompt = _active;
        _active = null;
        prompt.OnAccept = null;
        prompt.OnDeny = null;
        prompt.Close();
    }
}
