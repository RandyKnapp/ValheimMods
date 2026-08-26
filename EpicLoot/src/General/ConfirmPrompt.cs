using System;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// A <see cref="MessagePanelBase"/> whose Accept and Deny are supplied as callbacks by the caller,
/// so one prefab layout backs any number of yes/no questions. Subclasses exist only to give a
/// behaviour a name; they add no members.
/// </summary>
public abstract class ConfirmPrompt : MessagePanelBase
{
    public Action OnAccept;
    public Action OnDeny;

    /// <summary>
    /// Instantiates <paramref name="prefab"/> under <paramref name="parent"/> with the given
    /// already-localized title and body. Returns null when the prefab is missing from the bundle, in
    /// which case the caller must refuse the action rather than performing it unconfirmed.
    /// </summary>
    protected static T Create<T>(GameObject prefab, Transform parent, string title, string body)
        where T : ConfirmPrompt
    {
        if (prefab == null)
        {
            EpicLoot.LogWarningForce($"A message prefab is missing from the asset bundle, so the " +
                $"{typeof(T).Name} confirmation cannot be shown.");
            return null;
        }

        var panel = Instantiate(prefab, parent, false);
        panel.name = prefab.name;
        panel.transform.SetAsLastSibling();
        // Must be active before AddComponent: Unity only runs Awake (which wires the buttons) on
        // an active object, and the prefab may well have been authored hidden.
        panel.SetActive(true);

        var prompt = panel.AddComponent<T>();
        prompt.SetMessage(title, body);
        return prompt;
    }

    public override void OnAcceptClick()
    {
        // Clear both callbacks first: Close() only destroys at the end of the frame, so a stray
        // second click before then must not run the action twice.
        var accepted = OnAccept;
        OnAccept = null;
        OnDeny = null;

        Close();
        accepted?.Invoke();
    }

    public override void OnDenyClick()
    {
        var denied = OnDeny;
        OnAccept = null;
        OnDeny = null;

        Close();
        denied?.Invoke();
    }
}
