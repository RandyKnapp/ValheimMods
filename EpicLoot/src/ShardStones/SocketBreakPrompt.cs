using UnityEngine;

namespace EpicLoot.ShardStones {
    // A prompt that asks the player to confirm breaking a socketed stone, when breaking shards is enabled for removal.
    public sealed class SocketBreakPrompt : ConfirmPrompt {
        /// <summary>
        /// Instantiates the prompt under `parent` with the given already-localized title and body.
        /// Returns null when the prefab is missing from the bundle, in which case the caller must
        /// refuse the removal rather than destroying anything unconfirmed.
        /// </summary>
        public static SocketBreakPrompt Create(Transform parent, string title, string body) {
            return Create<SocketBreakPrompt>(EpicAssets.SocketMessagePrefab, parent, title, body);
        }
    }
}
