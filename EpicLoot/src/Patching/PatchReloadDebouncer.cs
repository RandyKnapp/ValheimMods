using EpicLoot.Config;
using JetBrains.Annotations;
using System;
using UnityEngine;

namespace EpicLoot.Patching
{
    /// <summary>
    /// Collapses the burst of file events one patch edit produces into a single rebuild.
    ///
    /// A save is rarely one event: editors write, flush and touch metadata separately, atomic savers
    /// write a temp file and rename it, and saving several patches at once raises a set per file.
    /// Rebuilding on each of them re-reads every patch and rewrites every target config -- expensive,
    /// and it makes the game hitch mid-edit.
    ///
    /// Trailing edge, deliberately: the first event of a save often arrives while the file is still
    /// empty or half written, so acting on it and ignoring the rest (a leading-edge debounce) would
    /// parse a truncated patch and then never look again.
    /// </summary>
    internal class PatchReloadDebouncer : MonoBehaviour
    {
        /// <summary>How long the patches folder must be quiet before the rebuild runs.</summary>
        private const float QuietPeriodSeconds = 0.5f;

        private static PatchReloadDebouncer _instance;

        /// <summary>Requests a rebuild, pushing back any rebuild already scheduled.</summary>
        internal static void Schedule()
        {
            // Unity's null check, not C#'s: the host object is destroyed with the rest of the scene
            // objects if the game ever tears down DontDestroyOnLoad, and the stale reference then
            // compares equal to null and is replaced.
            if (_instance == null)
            {
                GameObject host = new GameObject("EL_PatchReloadDebouncer");
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<PatchReloadDebouncer>();
            }

            _instance.CancelInvoke(nameof(Run));
            _instance.Invoke(nameof(Run), QuietPeriodSeconds);
        }

        [UsedImplicitly]
        private void Run()
        {
            try
            {
                ELConfig.RunPatchHotReload();
            }
            catch (Exception e)
            {
                // This runs off Unity's Invoke, so an escaping exception is reported against the
                // timer with no indication that a patch reload was what failed.
                EpicLoot.LogErrorForce($"Rebuilding configs from the patch files failed.\n{e}");
            }
        }
    }
}
