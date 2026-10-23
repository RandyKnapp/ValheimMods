using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EpicLoot.LegendarySystem
{
    public static class SetAura
    {
        // The full set the wearer shows, by ID, for every client to draw: this aura, and SetVisuals' glow and aura.
        private const string ZdoKey = "el-aura";
        internal static readonly int ZdoHash = ZdoKey.GetStableHashCode();

        private class AuraState
        {
            public string SetID = "";
            public readonly List<GameObject> Instances = new List<GameObject>();
        }

        private static readonly ConditionalWeakTable<Player, AuraState> States = new ConditionalWeakTable<Player, AuraState>();
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();

        public static void Publish(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.m_nview == null ||
                !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = "";
            foreach (LegendarySetProgress progress in SetBonusEvaluator.GetVisibleSetProgress(player))
            {
                if (progress.IsFull && HasVisuals(progress.Set))
                {
                    setID = progress.Set.ID;
                    break;
                }
            }

            if (zdo.GetString(ZdoHash) != setID)
            {
                zdo.Set(ZdoHash, setID);
            }

            Refresh(player);
        }

        public static bool HasVisuals(LegendarySetInfo set)
        {
            return set != null && (set.FullSetFx?.Count > 0 || set.WeaponGlow != null || set.HandGlow != null ||
                set.DrawArrow != null || set.BodyAura != null);
        }

        public static void Refresh(Player player)
        {
            if (player == null || player.m_nview == null || !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = zdo.GetString(ZdoHash);
            AuraState state = States.GetOrCreateValue(player);
            if (state.SetID == setID)
            {
                return;
            }

            foreach (GameObject instance in state.Instances)
            {
                if (instance != null)
                {
                    Object.Destroy(instance);
                }
            }

            state.Instances.Clear();
            state.SetID = setID;

            if (string.IsNullOrEmpty(setID) || !UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo set) ||
                set.FullSetFx == null)
            {
                return;
            }

            foreach (string fx in set.FullSetFx)
            {
                GameObject instance = Attach(player, fx);
                if (instance != null)
                {
                    state.Instances.Add(instance);
                }
            }
        }

        private static GameObject Attach(Player player, string fx)
        {
            // "Prefab/child/path" takes a child of the prefab, for effects that only exist inside a creature's rig. A
            // trailing "@0.5" draws it that much closer around the wearer (see Spread).
            string path = fx;
            float spread = 1f;
            int at = fx.LastIndexOf('@');
            if (at > 0 && float.TryParse(fx.Substring(at + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                path = fx.Substring(0, at);
                spread = Mathf.Clamp(parsed, 0.05f, 10f);
            }

            int slash = path.IndexOf('/');
            string prefabName = slash > 0 ? path.Substring(0, slash) : path;
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefabName) : null;
            Transform child = prefab != null && slash > 0 ? prefab.transform.Find(path.Substring(slash + 1)) : null;
            GameObject source = slash > 0 ? child?.gameObject : prefab;
            if (source == null)
            {
                if (MissingLogged.Add(fx))
                {
                    EpicLoot.LogWarning($"[SetAura] could not find '{fx}'; the set aura shows without it.");
                }

                return null;
            }

            GameObject instance;
            ZNetView.m_forceDisableInit = true;
            try
            {
                // Each client builds its own copy from the wearer's ZDO, so the prefab's ZNetView must not
                // register this one as a networked object.
                instance = Object.Instantiate(source, player.GetCenterPoint(), player.transform.rotation, player.transform);
            }
            finally
            {
                ZNetView.m_forceDisableInit = false;
            }

            instance.name = $"EL_SetAura_{fx}";

            // Vanilla effects made to end on their own would take the aura with them.
            foreach (TimedDestruction timed in instance.GetComponentsInChildren<TimedDestruction>(true))
            {
                Object.Destroy(timed);
            }

            if (!Mathf.Approximately(spread, 1f))
            {
                Spread(instance.transform, spread);
            }

            return instance;
        }

        // Pulls an effect in towards (or pushes it out from) its middle: every emitter's shape and every part's offset
        // from the effect's root scale by factor, while the particles keep their size and speed. A creature's effect
        // brought onto a player can spread far wider than the player (Eikthyr's antler sparks reach about 2 m out).
        private static void Spread(Transform root, float factor)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
            {
                if (part != root)
                {
                    part.localPosition *= factor;
                }
            }

            foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.ShapeModule shape = system.shape;
                shape.position *= factor;
                shape.scale *= factor;
                shape.radius *= factor;
            }
        }
    }
}
