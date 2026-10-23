using Jotunn;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Audio;

namespace Common {
    /// <summary>
    /// Routes a prefab's sound into Valheim's own audio mixer by resolving its JVLmock_ mixer groups.
    ///
    /// Since the 1.0 update the master volume slider no longer sets AudioListener.volume (vanilla pins it to
    /// 1); master x SFX is written into the SfxVol/GuiVol parameters of vanilla's MasterMixer instead. So an
    /// AudioSource that does not output into that mixer ignores both the master and the SFX sliders.
    ///
    /// Prefabs in ValheimUnity route their AudioSources through JVLmock_MasterMixer (JVLmock_SFX,
    /// JVLmock_SFX_LARGE, JVLmock_Ambient, JVLmock_GUI), and Jotunn swaps each group for the vanilla group of
    /// the same name. Jotunn does that by itself for Custom* entities created with fixReference: true; this is
    /// for everything else -- bundle prefabs a mod instantiates or registers without fixing references.
    /// </summary>
    public static class AudioMixerMocks {
        /// <summary>
        /// Resolves the mock mixer group of every AudioSource under <paramref name="root"/>, inactive children
        /// included. A source with no output group at all is sent to <paramref name="fallbackGroup"/> (a vanilla
        /// group name such as "SFX" or "Ambient") when one is given, so a bundle built before its mocks were
        /// assigned still plays through the mixer. Needs the vanilla mixer loaded: call it from
        /// PrefabManager.OnPrefabsRegistered or later. Idempotent.
        /// </summary>
        public static void Resolve(GameObject root, string fallbackGroup = null) {
            if (root == null) { return; }

            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true)) {
                source.FixReferences();

                if (source.outputAudioMixerGroup == null && fallbackGroup != null) {
                    source.outputAudioMixerGroup = PrefabManager.Cache.GetPrefab<AudioMixerGroup>(fallbackGroup);
                }
            }
        }
    }
}
