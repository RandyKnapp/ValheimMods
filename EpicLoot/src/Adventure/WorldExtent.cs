using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Adventure
{
    /// <summary>
    /// The one place that answers "how big is this world".
    ///
    /// Nothing in the adventure code may read <c>WorldGenerator.worldSize</c> or
    /// <c>WorldGenerator.waterEdge</c>. Both are <c>public const</c>, so the C# compiler inlines
    /// 10000/10500 into EpicLoot.dll at build time -- no field exists at runtime for a world-size mod
    /// to patch, and vanilla itself never reads either one (every real check uses an inline literal,
    /// which is exactly why Expand World Size has to transpile them). Reading one silently reports a
    /// 10km world on a 40km map.
    /// </summary>
    internal static class WorldExtent
    {
        /// <summary>The radius vanilla ships. Everything here is expressed relative to it.</summary>
        internal const float VanillaPlayableRadius = 10000f;
        internal const float VanillaTotalRadius = 10500f;

        /// <summary>
        /// Expand World Size's default edge band. Only used by the probe fallback, which can measure
        /// the total radius exactly but has no way to see where the edge band starts.
        /// </summary>
        private const float AssumedEdgeSize = 500f;

        /// <summary>Terrain generates out to here. Vanilla 10000.</summary>
        internal static float PlayableRadius { get; private set; } = VanillaPlayableRadius;

        /// <summary>Past here height is hard-clamped to -400. Vanilla 10500.</summary>
        internal static float TotalRadius { get; private set; } = VanillaTotalRadius;

        /// <summary>
        /// Biome feature-size multiplier. A bigger radius alone means *more* biome patches of the
        /// same size; only stretch makes the patches themselves bigger. Vanilla 1.
        /// </summary>
        internal static float Stretch { get; private set; } = 1f;

        /// <summary>Where the numbers came from, for the console command and the log line.</summary>
        internal static string Source { get; private set; } = "vanilla";

        /// <summary>
        /// Factor for rescaling config radii that were authored in metres against a vanilla world.
        /// </summary>
        internal static float RadiusScale => PlayableRadius / VanillaPlayableRadius;

        /// <summary>
        /// A probe result this close to the vanilla radius is reported as vanilla, so an unmodded
        /// install gets an exact scale of 1 and a clean log line instead of 10499.98m.
        /// </summary>
        private const float VanillaSnapTolerance = 50f;

        /// <summary>
        /// The probe walks real terrain heights, so it is not free. Expand World Size -- the case
        /// that actually resizes a world mid-session -- is read through the cheap reflection path
        /// above, so the probe only needs to run often enough to notice some other mod doing it.
        /// </summary>
        private const float ProbeIntervalSeconds = 30f;

        private static bool _probed;
        private static float _probedAt;
        private static bool _probeSucceeded;
        private static float _probedTotalRadius;

        private static bool _resolvedEws;
        private static MethodInfo _ewsWorldRadius;
        private static MethodInfo _ewsTotalRadius;
        private static MethodInfo _ewsStretch;

        /// <summary>
        /// Re-resolves the world extent. Cheap enough to call on every merchant open and every spawn
        /// point request, which is what catches a mid-session resize: Expand World Size can be
        /// re-synchronized from the server or hot-reloaded from disk at any time, and the world seed
        /// does not change when it happens.
        /// </summary>
        internal static void Refresh()
        {
            float playable = VanillaPlayableRadius;
            float total = VanillaTotalRadius;
            float stretch = 1f;
            string source = "vanilla";

            if (TryReadExpandWorldSize(ref playable, ref total, ref stretch))
            {
                source = "expand_world_size";
            }
            else if (TryProbeWorldEdgeThrottled(out float probedTotal))
            {
                if (Mathf.Abs(probedTotal - VanillaTotalRadius) <= VanillaSnapTolerance)
                {
                    // An unmodded world. Use the exact constants rather than the probe's
                    // within-a-metre answer, so RadiusScale is exactly 1 and nothing downstream
                    // rescales a config band by 0.99999.
                    total = VanillaTotalRadius;
                    playable = VanillaPlayableRadius;
                }
                else
                {
                    total = probedTotal;
                    playable = Mathf.Max(1f, probedTotal - AssumedEdgeSize);
                    source = "probe";
                }
            }

            // A hand-broken config or a mod mid-reload can hand back nonsense; refusing it keeps the
            // previous good values rather than poisoning every downstream radius.
            if (!(playable > 0f) || !(total >= playable) || !(stretch > 0f))
            {
                EpicLoot.LogWarningForce($"Ignoring implausible world extent from {source}: " +
                    $"playable={playable}, total={total}, stretch={stretch}. " +
                    $"Keeping playable={PlayableRadius}, total={TotalRadius}, stretch={Stretch}.");
                return;
            }

            if (Mathf.Approximately(playable, PlayableRadius) &&
                Mathf.Approximately(total, TotalRadius) &&
                Mathf.Approximately(stretch, Stretch) &&
                source == Source)
            {
                return;
            }

            PlayableRadius = playable;
            TotalRadius = total;
            Stretch = stretch;
            Source = source;

            EpicLoot.LogForce($"Adventure world extent: {Describe()}");
        }

        /// <summary>
        /// Forgets the cached probe. Called on world change, where the throttle would otherwise keep
        /// reporting the previous world's size for up to <see cref="ProbeIntervalSeconds"/>.
        /// </summary>
        internal static void InvalidateProbe()
        {
            _probed = false;
            _probeSucceeded = false;
            _probedTotalRadius = VanillaTotalRadius;
        }

        internal static string Describe()
        {
            return $"playable={PlayableRadius:0.#}m total={TotalRadius:0.#}m " +
                $"stretch={Stretch:0.###} scale={RadiusScale:0.###} (source: {Source})";
        }

        /// <summary>
        /// Reads Expand World Size's own configuration through its plugin instance. Soft: no assembly
        /// reference, no BepInDependency, and any failure falls through to the probe. EWS is
        /// authoritative when stacked with Expand World Data or Better Continents -- it pushes its
        /// size into both of them -- so it is the right thing to ask when present.
        /// </summary>
        private static bool TryReadExpandWorldSize(ref float playable, ref float total, ref float stretch)
        {
            try
            {
                if (!_resolvedEws)
                {
                    _resolvedEws = true;

                    if (Chainloader.PluginInfos.TryGetValue("expand_world_size", out var info) &&
                        info?.Instance != null)
                    {
                        var configType = info.Instance.GetType().Assembly
                            .GetType("ExpandWorldSize.Configuration");
                        if (configType != null)
                        {
                            _ewsWorldRadius = AccessTools.PropertyGetter(configType, "WorldRadius");
                            _ewsTotalRadius = AccessTools.PropertyGetter(configType, "WorldTotalRadius");
                            _ewsStretch = AccessTools.PropertyGetter(configType, "WorldStretch");
                        }
                    }
                }

                if (_ewsWorldRadius == null || _ewsTotalRadius == null || _ewsStretch == null)
                {
                    return false;
                }

                playable = (float)_ewsWorldRadius.Invoke(null, null);
                total = (float)_ewsTotalRadius.Invoke(null, null);
                stretch = (float)_ewsStretch.Invoke(null, null);
                return true;
            }
            catch (Exception e)
            {
                // Their internals moved, or the config is mid-reload. Degrade to the probe rather
                // than taking the adventure system down with it.
                EpicLoot.LogWarning($"Could not read Expand World Size configuration ({e.Message}); " +
                    "falling back to probing the world edge.");
                _ewsWorldRadius = null;
                _ewsTotalRadius = null;
                _ewsStretch = null;
                return false;
            }
        }

        /// <summary>
        /// <see cref="TryProbeWorldEdge"/>, rate limited, and re-probed from scratch whenever the
        /// world changes.
        /// </summary>
        private static bool TryProbeWorldEdgeThrottled(out float totalRadius)
        {
            if (_probed && Time.unscaledTime - _probedAt < ProbeIntervalSeconds)
            {
                totalRadius = _probedTotalRadius;
                return _probeSucceeded;
            }

            _probed = true;
            _probedAt = Time.unscaledTime;
            _probeSucceeded = TryProbeWorldEdge(out _probedTotalRadius);
            totalRadius = _probedTotalRadius;
            return _probeSucceeded;
        }

        /// <summary>
        /// Measures the world edge directly, so it works against any world-size mod rather than only
        /// the one we know by name. GetBiomeHeight hard-returns -2 * GetHeightMultiplier() (-400)
        /// past the total radius, and nothing inside the world comes close -- the deepest ocean is
        /// about -26 -- so that value is an exact sentinel. Binary search costs ~40 GetHeight calls.
        /// </summary>
        private static bool TryProbeWorldEdge(out float totalRadius)
        {
            totalRadius = VanillaTotalRadius;

            var worldGenerator = WorldGenerator.instance;
            if (worldGenerator == null)
            {
                return false;
            }

            float outsideHeight = -2f * WorldGenerator.GetHeightMultiplier();
            const float searchCeiling = 1000000f;

            // Establish a bracket: inside must be in-world, outside must be past the edge.
            float inside = 0f;
            float outside = VanillaTotalRadius;
            while (!IsOutside(worldGenerator, outside, outsideHeight))
            {
                inside = outside;
                outside *= 2f;
                if (outside > searchCeiling)
                {
                    // No edge anywhere sane. Something else is patching heights; leave the caller on
                    // its previous values rather than inventing a world size.
                    return false;
                }
            }

            // A world smaller than vanilla is legal too, so do not assume the vanilla radius is inside.
            if (IsOutside(worldGenerator, inside, outsideHeight))
            {
                inside = 0f;
            }

            for (int i = 0; i < 40 && outside - inside > 1f; i++)
            {
                float mid = (inside + outside) * 0.5f;
                if (IsOutside(worldGenerator, mid, outsideHeight))
                {
                    outside = mid;
                }
                else
                {
                    inside = mid;
                }
            }

            totalRadius = inside;
            return totalRadius > 0f;
        }

        private static bool IsOutside(WorldGenerator worldGenerator, float radius, float outsideHeight)
        {
            // Sampled on two axes: the edge test is a pure radius comparison today, but two samples
            // cost nothing at 40 iterations and a directional edge would otherwise read as noise.
            return Mathf.Approximately(worldGenerator.GetHeight(radius, 0f), outsideHeight) &&
                Mathf.Approximately(worldGenerator.GetHeight(0f, radius), outsideHeight);
        }
    }
}
