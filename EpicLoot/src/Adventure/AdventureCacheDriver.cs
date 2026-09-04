using System.Collections;
using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// Coroutine host for adventure work that must outlive the UI that started it.
    ///
    /// The merchant panel cannot run these -- StoreGui deactivates that object on close, which would
    /// stop a coroutine partway -- and Player.m_localPlayer is no better: a coroutine hosted there
    /// dies with the player object on death, logout or world change, leaving the caller's completion
    /// callback to never run at all.
    ///
    /// This object is DontDestroyOnLoad, so the only thing that stops its coroutines is
    /// <see cref="StopAll"/>, which BountyLocationEarlyCache.Reset calls on world change.
    /// </summary>
    internal class AdventureCacheDriver : MonoBehaviour
    {
        private static AdventureCacheDriver _instance;

        public static void Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                var go = new GameObject("EpicLoot_AdventureCacheDriver");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<AdventureCacheDriver>();
            }

            _instance.StartCoroutine(routine);
        }

        public static void StopAll()
        {
            if (_instance != null)
            {
                _instance.StopAllCoroutines();
            }
        }
    }
}
