using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string QuarryMark = nameof(QuarryMark);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // QuarryMark (4-piece Ullr's Hunt set bonus, NoRoll). An arrow from a drawn bow marks the creature it hits for
    // MarkDuration seconds. The marker's next melee hit on it with an axe or a polearm (atgeir) uses the mark up: that
    // hit deals the effect value as a percent more damage and gives back StaminaRefund stamina.
    //
    // Everything runs on the marker's client. The arrow is placed on the hit in SharedCharacterDamagePatch's postfix,
    // where HitSource.FiringWeapon is the bow; the mark is used up in its prefix, which changes the outgoing hit, so the
    // bonus travels inside the hit and works on a creature another client owns. A mark keeps the value it was placed
    // with, so it can be used after swapping from the set bow to an axe, which drops the 4-piece. Marks are keyed by the
    // creature's ZDOID and matter to no one else, so nothing is networked; the marker over the creature is local too.
    public static class QuarryMark
    {
        // Both tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMarkDuration = 10f;
        public const float DefaultStaminaRefund = 25f;

        private const string MarkDurationKey = "MarkDuration";
        private const string StaminaRefundKey = "StaminaRefund";

        private static float MarkDuration => EffectConfig.GetClamped(MagicEffectType.QuarryMark, MarkDurationKey,
            DefaultMarkDuration, 1f, 60f);
        private static float StaminaRefund => EffectConfig.GetClamped(MagicEffectType.QuarryMark, StaminaRefundKey,
            DefaultStaminaRefund, 0f, 200f);

        // Vanilla fx, played as local copies: the archery target's bullseye where the arrow lands, its bigger burst and
        // the crit flash (with its sound) where the mark is used up.
        private const string MarkFxPrefab = "vfx_archerytarget_bullseye";
        private const string ConsumeFxPrefab = "vfx_archerytarget_bullseye_quint";
        private const string ConsumeCritFxPrefab = "fx_crit";

        private struct Mark
        {
            public float Until;
            public float Value;
            public QuarryMarker Marker;
        }

        private static readonly Dictionary<ZDOID, Mark> Marks = new Dictionary<ZDOID, Mark>();
        private static readonly List<ZDOID> Expired = new List<ZDOID>();
        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();
        private static Player _owner;

        // Tooltip: "Your arrows mark creatures for {1}s. ... deals +{0}% damage and restores {2} stamina."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.QuarryMark, value => new object[]
            {
                value, MarkDuration, StaminaRefund
            });
        }

        // Marks belong to the local player that placed them; a respawn or a new character starts clean.
        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            foreach (var mark in Marks.Values)
            {
                DestroyMarker(mark.Marker);
            }
            Marks.Clear();
        }

        private static void PruneExpired()
        {
            float now = Time.time;
            Expired.Clear();
            foreach (var pair in Marks)
            {
                if (now > pair.Value.Until)
                {
                    Expired.Add(pair.Key);
                }
            }

            foreach (var id in Expired)
            {
                DestroyMarker(Marks[id].Marker);
                Marks.Remove(id);
            }
        }

        // Called by SharedCharacterDamagePatch's postfix, for weapon strikes.
        public static void OnDamageDealt(Character target, HitData hit, Character attacker)
        {
            if (attacker == null || attacker != Player.m_localPlayer || target == null || target is Player ||
                target.IsTamed() || target.IsDead() || target.m_nview == null || !target.m_nview.IsValid() ||
                !ElementalArchery.IsDrawnBow(HitSource.FiringWeapon))
            {
                return;
            }

            var player = (Player)attacker;
            if (!player.HasActiveMagicEffect(MagicEffectType.QuarryMark, out float value))
            {
                return;
            }

            SyncOwner(player);
            PruneExpired();

            ZDOID id = target.GetZDOID();
            float until = Time.time + MarkDuration;
            Marks.TryGetValue(id, out Mark mark);
            mark.Until = until;
            mark.Value = value;
            if (mark.Marker == null)
            {
                mark.Marker = QuarryMarker.Create(target);
            }
            if (mark.Marker != null)
            {
                mark.Marker.Until = until;
            }
            Marks[id] = mark;

            SpawnLocalFx(MarkFxPrefab, hit.m_point != Vector3.zero ? hit.m_point : target.GetCenterPoint());
        }

        // Called by SharedCharacterDamagePatch's prefix, for weapon strikes: changes the outgoing hit.
        public static void ModifyOutgoingHit(Character target, HitData hit, Character attacker)
        {
            if (Marks.Count == 0 || attacker == null || attacker != Player.m_localPlayer || target == null ||
                HitSource.FiringWeapon != null || target.m_nview == null || !target.m_nview.IsValid())
            {
                return;
            }

            var player = (Player)attacker;
            var weapon = MagicEffectsHelper.GetActiveWeapon(player);
            if (weapon == null)
            {
                return;
            }

            var skill = weapon.m_shared.m_skillType;
            if (skill != Skills.SkillType.Axes && skill != Skills.SkillType.Polearms)
            {
                return;
            }

            ZDOID id = target.GetZDOID();
            if (!Marks.TryGetValue(id, out Mark mark))
            {
                return;
            }

            Marks.Remove(id);
            DestroyMarker(mark.Marker);
            if (Time.time > mark.Until)
            {
                return;
            }

            hit.ApplyModifier(1f + mark.Value / 100f);
            float refund = StaminaRefund;
            if (refund > 0f)
            {
                player.AddStamina(refund);
            }

            Vector3 point = hit.m_point != Vector3.zero ? hit.m_point : target.GetCenterPoint();
            SpawnLocalFx(ConsumeFxPrefab, point);
            SpawnLocalFx(ConsumeCritFxPrefab, point);
            EpicLoot.Log($"[QuarryMark] Mark used up on {target.m_name}: +{mark.Value:0.#}% damage, +{refund:0.#} stamina.");
        }

        private static void DestroyMarker(QuarryMarker marker)
        {
            if (marker != null)
            {
                Object.Destroy(marker.gameObject);
            }
        }

        // World load (SetEffectWarmup): the local copies a mark and its use play from.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (!drawn || ZNetScene.instance == null)
            {
                return;
            }

            foreach (var name in new[] { MarkFxPrefab, ConsumeFxPrefab, ConsumeCritFxPrefab })
            {
                LocalFx.Prewarm(ZNetScene.instance.GetPrefab(name), scaled: false);
            }
        }

        private static void SpawnLocalFx(string name, Vector3 position)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null)
            {
                if (MissingFxLogged.Add(name))
                {
                    EpicLoot.LogWarning($"[QuarryMark] could not find the '{name}' prefab; playing without it.");
                }
                return;
            }

            LocalFx.Spawn(prefab, position, Quaternion.identity);
        }
    }

    // The red ping floating over a marked creature, seen by the marker only. It follows the creature's top rather than
    // being parented to it, so a creature's own scale never stretches it, and goes away with the mark, the creature or
    // its timer, whichever is first.
    public class QuarryMarker : MonoBehaviour
    {
        private const float HeightAboveTop = 0.6f;
        private const float WorldSize = 0.45f;
        private static readonly Color Tint = new Color(1f, 0.25f, 0.2f, 0.95f);

        private static Sprite _sprite;
        private static bool _spriteMissingLogged;

        public float Until;
        private Character _target;

        internal static QuarryMarker Create(Character target)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return null;
            }

            Sprite sprite = GetSprite();
            if (sprite == null)
            {
                return null;
            }

            var go = new GameObject("EL_QuarryMarker");
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Tint;
            float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            if (size > 0f)
            {
                go.transform.localScale = Vector3.one * (WorldSize / size);
            }

            var marker = go.AddComponent<QuarryMarker>();
            marker._target = target;
            marker.Follow();
            return marker;
        }

        // The minimap's ping icon, the round marker vanilla draws where someone pings the map.
        private static Sprite GetSprite()
        {
            if (_sprite == null && Minimap.instance != null)
            {
                _sprite = Minimap.instance.GetSprite(Minimap.PinType.Ping);
            }

            if (_sprite == null && !_spriteMissingLogged)
            {
                _spriteMissingLogged = true;
                EpicLoot.LogWarning("[QuarryMark] could not find the minimap ping icon; marked creatures show no marker.");
            }
            return _sprite;
        }

        private void LateUpdate()
        {
            if (_target == null || _target.IsDead() || Time.time > Until)
            {
                Destroy(gameObject);
                return;
            }

            Follow();
        }

        private void Follow()
        {
            transform.position = _target.GetTopPoint() + Vector3.up * HeightAboveTop;
            Camera camera = Utils.GetMainCamera();
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            }
        }
    }
}
