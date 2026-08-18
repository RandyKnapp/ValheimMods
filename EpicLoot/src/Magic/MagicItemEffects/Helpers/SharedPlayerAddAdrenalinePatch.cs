using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Single consolidated Harmony patch for Player.AddAdrenaline. It detects the moment the local player's
    // adrenaline "activates" -- i.e. a gain fills the pool -- and fans that out to every effect that procs
    // on it. The detection is subtle enough (two distinct vanilla outcomes, plus a degen false-positive to
    // rule out) that duplicating it per effect would invite the copies to drift apart.
    //
    // Each effect keeps its own guard (has-effect / rate limit) inside its handler, so the order among
    // handlers is not load-bearing.
    [HarmonyPatch(typeof(Player), nameof(Player.AddAdrenaline))]
    internal static class SharedPlayerAddAdrenalinePatch {
        // Captured across the AddAdrenaline call: the pool before the change, and whether this call was a gain
        // (v > 0). Only a gain can fill the pool, so guarding on it rules out the per-frame degen decrements.
        private struct AdrenalineChange {
            public float Before;
            public bool WasGain;
        }

        [HarmonyPrefix]
        private static void PreAddAdrenaline(Player __instance, float v, out AdrenalineChange __state) {
            __state = new AdrenalineChange { Before = __instance.GetAdrenaline(), WasGain = v > 0f };
        }

        [HarmonyPostfix]
        private static void PostAddAdrenaline(Player __instance, AdrenalineChange __state) {
            if (__instance != Player.m_localPlayer) {
                return;
            }

            var max = __instance.GetMaxAdrenaline();
            if (max <= 0f) {
                return; // no adrenaline source -> inert (matches the other adrenaline shards)
            }

            var after = __instance.GetAdrenaline();

            // Adrenaline "activated" this call if a gain pushed it to full. Two vanilla outcomes:
            //   - gear WITHOUT a full-adrenaline SE caps the pool at max        -> after >= max
            //   - gear WITH a full-adrenaline SE pops and resets the pool to 0  -> a substantial pool
            //     dropped to ~0. Guarding on WasGain rules out the per-frame degen decrements, which are
            //     the only other way the pool falls.
            var cappedToMax = __state.Before < max && after >= max;
            var poppedToZero = __state.WasGain && __state.Before > 1f && after <= 0.01f;
            if (!cappedToMax && !poppedToZero) {
                return;
            }

            SummonBatWhenActivatingAdrenaline.OnAdrenalineActivated(__instance);
            AdrenalineCharge.OnAdrenalineActivated(__instance);
            AdrenalineFrostWave.OnAdrenalineActivated(__instance);
            AdrenalineIncreasesHealthRegen.OnAdrenalineActivated(__instance);
        }
    }
}
