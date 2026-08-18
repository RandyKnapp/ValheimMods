using System;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // A prompt that asks the player to confirm breaking a socketed stone, when breaking shards is enabled for removal.
    public sealed class SocketBreakPrompt : MessagePanelBase {
        public Action OnAccept;
        public Action OnDeny;

        /// <summary>
        /// Instantiates the prompt under `parent` with the given already-localized title and body.
        /// Returns null when the prefab is missing from the bundle, in which case the caller must
        /// refuse the removal rather than destroying anything unconfirmed.
        /// </summary>
        public static SocketBreakPrompt Create(Transform parent, string title, string body) {
            if (EpicAssets.SocketMessagePrefab == null) {
                EpicLoot.LogWarningForce("The SocketMessage prefab is missing from the asset bundle, " +
                    "so socketed stones cannot be broken.");
                return null;
            }

            var panel = Instantiate(EpicAssets.SocketMessagePrefab, parent, false);
            panel.name = "SocketMessage";
            panel.transform.SetAsLastSibling();
            // Must be active before AddComponent: Unity only runs Awake (which wires the buttons) on
            // an active object, and the prefab may well have been authored hidden.
            panel.SetActive(true);

            var prompt = panel.AddComponent<SocketBreakPrompt>();
            prompt.SetMessage(title, body);
            return prompt;
        }

        public override void OnAcceptClick() {
            // Clear both callbacks first: Close() only destroys at the end of the frame, so a stray
            // second click before then must not run the break twice.
            var accepted = OnAccept;
            OnAccept = null;
            OnDeny = null;

            Close();
            accepted?.Invoke();
        }

        public override void OnDenyClick() {
            var denied = OnDeny;
            OnAccept = null;
            OnDeny = null;

            Close();
            denied?.Invoke();
        }
    }
}
