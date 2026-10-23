using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string KineticQuake = nameof(KineticQuake);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Kinetic Quake (4-piece Hrungnir's Stand set bonus, NoRoll). Every attack the player blocks stores a charge; the
    // effect value is how many it takes (16 / 14 / 12 at Legendary / Mythic / Ancient). Once charged, the next attack
    // blocked releases a rock shockwave: a line of eruptions racing Length metres along the blocking direction, hitting
    // every enemy in a Width-wide band once, as the wave reaches it, for DamagePercent of the shield's block power as
    // blunt, with stagger and knockback.
    //
    // What counts: a held block -- BlockAttack returned true, the guard did not break (AddStaggerDamage said no) and
    // stamina remained, which is vanilla's own success flag -- against an attack that has an attacker, vanilla's
    // condition for block adrenaline. Guard breaks never count, though vanilla grants adrenaline for them. Armed is
    // read before the block, so the block that fills the bar does not also release it, and the releasing block adds no
    // charge. The quake's own hits and staggers are not blocks, so they never refill it.
    //
    // The charge never decays. It is kept while the set is off, and saved in the character's custom data, so it
    // survives logout; a static count carries it across a respawn in the same session.
    //
    // Multiplayer: blocking runs on the blocker's own client, so everything happens there. The damage is dealt by that
    // client as a bonus hit (see HitSource). One RPC sends the wave's origin, direction and step count to every client,
    // which walks the same steps and plays the eruptions locally. The charge level shows on every client as 1-5 rocks
    // orbiting the player, from an int the owner mirrors to the player ZDO.
    public static class KineticQuake
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultDamagePercent = 300f;  // of the shield's current block power
        public const float DefaultLength = 12f;          // metres the wave travels
        public const float DefaultWidth = 4f;            // metres; the full width of the band it hits
        public const float DefaultStepSpacing = 2f;      // metres between eruptions
        public const float DefaultWaveSpeed = 30f;       // metres per second
        public const float DefaultPushForce = 80f;
        public const float DefaultStaggerMultiplier = 2f;
        public const float DefaultMaxStepRise = 2.5f;    // metres the ground may rise or drop between two eruptions
        public const float DefaultFxScale = 0.5f;        // the Elder's stomp at full size has a 14 m ring

        private const string DamagePercentKey = "DamagePercent";
        private const string LengthKey = "Length";
        private const string WidthKey = "Width";
        private const string StepSpacingKey = "StepSpacing";
        private const string WaveSpeedKey = "WaveSpeed";
        private const string PushForceKey = "PushForce";
        private const string StaggerMultiplierKey = "StaggerMultiplier";
        private const string MaxStepRiseKey = "MaxStepRise";
        private const string FxScaleKey = "FxScale";

        private static float DamagePercent => EffectConfig.GetClamped(MagicEffectType.KineticQuake, DamagePercentKey, DefaultDamagePercent, 0f, 2000f);
        private static float Length => EffectConfig.GetClamped(MagicEffectType.KineticQuake, LengthKey, DefaultLength, 2f, 30f);
        private static float Width => EffectConfig.GetClamped(MagicEffectType.KineticQuake, WidthKey, DefaultWidth, 1f, 10f);
        private static float StepSpacing => EffectConfig.GetClamped(MagicEffectType.KineticQuake, StepSpacingKey, DefaultStepSpacing, 0.5f, 5f);
        private static float WaveSpeed => EffectConfig.GetClamped(MagicEffectType.KineticQuake, WaveSpeedKey, DefaultWaveSpeed, 5f, 100f);
        private static float PushForce => EffectConfig.GetClamped(MagicEffectType.KineticQuake, PushForceKey, DefaultPushForce, 0f, 300f);
        private static float StaggerMultiplier => EffectConfig.GetClamped(MagicEffectType.KineticQuake, StaggerMultiplierKey, DefaultStaggerMultiplier, 0f, 10f);
        private static float MaxStepRise => EffectConfig.GetClamped(MagicEffectType.KineticQuake, MaxStepRiseKey, DefaultMaxStepRise, 0.5f, 10f);
        private static float FxScale => EffectConfig.GetClamped(MagicEffectType.KineticQuake, FxScaleKey, DefaultFxScale, 0.1f, 2f);

        // Tooltip: "... At {0} charges, the next attack you block releases a rock shockwave {2}m ahead, dealing {1}% of
        // your block power as blunt damage."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.KineticQuake, value => new object[] { value, DamagePercent, Length });
        }

        internal const int MaxRocks = 5;
        internal static readonly int RocksZdoHash = "el-kqr".GetStableHashCode();
        private const string FxRpc = "el-kq";
        private const string CustomDataKey = "EpicLoot.KineticQuake";

        // The eruption: the Elder's stomp (dust, pebbles, rock chunks and a ground ring), or a troll's ground slam if it
        // is ever missing. One Elder stomp sound plays at the start of the wave, and each orbiting rock leaves a small
        // rock-break puff as it goes.
        private static readonly string[] EruptionFxPrefabs = { "vfx_gdking_stomp", "vfx_troll_groundslam" };
        private const string EruptionSfxPrefab = "sfx_gdking_stomp";
        private const string RockPuffFxPrefab = "vfx_RockDestroyed";
        private const float RockPuffScale = 0.5f;
        private const float LocalFxLifetime = 10f;           // a fallback, should a copy have no TimedDestruction
        private const float ShakeRange = 30f;
        private const float ShakeStrength = 2f;

        private const float HitHeight = 1f;                  // the band's centre line, above the ground
        private const float WallCheckHeight = 1f;            // a wall at this height between two eruptions stops the wave

        // State for the local player only. The charge itself outlives the Player object: a respawn in the same session
        // keeps it (by profile), and anything else reads it back from the character's custom data.
        private static Player _owner;
        private static int _charges;
        private static long _chargeProfileId;
        private static bool _chargeLoaded;
        private static int _publishedRocks = -1;

        // Across one BlockAttack call on the local player.
        private static bool _inBlock;
        private static bool _armed;
        private static bool _guardBroke;

        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();
        private static readonly Collider[] HitBuffer = new Collider[128];
        private static readonly RaycastHit[] GroundHits = new RaycastHit[16];
        private static readonly List<Vector3> StepPoints = new List<Vector3>();
        private static readonly List<Vector3> FxStepPoints = new List<Vector3>();

        private static int _groundMask;
        private static int _wallMask;
        private static int _characterMask;

        // Vanilla's Character.s_groundRayMask, which is only filled in once some character has woken.
        private static int GroundMask => _groundMask != 0 ? _groundMask : _groundMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain", "blocker", "vehicle");

        // Solid things a rolling wave can't pass: rock, buildings and invisible walls. Rising terrain is caught by the
        // height test instead, and trees and bushes let it through.
        private static int WallMask => _wallMask != 0 ? _wallMask : _wallMask = LayerMask.GetMask("static_solid",
            "piece", "blocker");

        // Characters only (vanilla Attack's character layers).
        private static int CharacterMask => _characterMask != 0 ? _characterMask : _characterMask = LayerMask.GetMask(
            "character", "character_net", "character_ghost", "hitbox", "character_noenv");

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _publishedRocks = -1;
            _inBlock = false;
            _armed = false;
            _guardBroke = false;

            long profileId = Game.instance != null && Game.instance.GetPlayerProfile() != null
                ? Game.instance.GetPlayerProfile().GetPlayerID()
                : 0L;
            if (_chargeLoaded && profileId == _chargeProfileId)
            {
                // A respawn: the new Player loaded the last saved custom data, which may be behind the live count.
                SaveCharges(player);
                return;
            }

            _chargeProfileId = profileId;
            _chargeLoaded = true;
            _charges = 0;
            if (player.m_customData.TryGetValue(CustomDataKey, out string saved) &&
                int.TryParse(saved, NumberStyles.Integer, CultureInfo.InvariantCulture, out int count))
            {
                _charges = Mathf.Max(0, count);
            }
        }

        private static void SaveCharges(Player player)
        {
            player.m_customData[CustomDataKey] = _charges.ToString(CultureInfo.InvariantCulture);
        }

        // The charges needed for the bonus the player is wearing, or 0 without it.
        private static int GetNeeded(Player player)
        {
            if (!player.HasActiveMagicEffect(MagicEffectType.KineticQuake, out float value) || value <= 0f)
            {
                return 0;
            }

            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        // For the HUD bar: false without the bonus. The charge kept from before may exceed what a lower-rarity set
        // needs; it shows (and fires) as full.
        internal static bool TryGetLocalState(Player player, out int charges, out int needed)
        {
            charges = 0;
            needed = 0;
            if (player == null)
            {
                return false;
            }

            SyncOwner(player);
            needed = GetNeeded(player);
            if (needed <= 0)
            {
                return false;
            }

            charges = Mathf.Min(_charges, needed);
            return true;
        }

        // 1 rock for the first charge, rising evenly to MaxRocks exactly when the bar is full.
        private static int GetRockCount(int charges, int needed)
        {
            if (needed <= 0 || charges <= 0)
            {
                return 0;
            }

            if (charges >= needed || needed <= 1)
            {
                return MaxRocks;
            }

            return 1 + Mathf.FloorToInt((MaxRocks - 1) * (charges - 1) / (float)(needed - 1));
        }

        // Written only on change, so the ZDO revision moves once per change rather than every frame.
        private static void PublishRocks(Player player)
        {
            int rocks = TryGetLocalState(player, out int charges, out int needed) ? GetRockCount(charges, needed) : 0;
            if (rocks == _publishedRocks)
            {
                return;
            }

            ZDO zdo = player.m_nview != null && player.m_nview.IsValid() ? player.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return;
            }

            _publishedRocks = rocks;
            zdo.Set(RocksZdoHash, rocks);
        }

        // Called from SharedHumanoidBlockAttackPatch's prefix. Armed is taken here, before the block, so the block that
        // fills the bar does not also release it.
        public static void BeforeBlock(Humanoid blocker)
        {
            Player player = Player.m_localPlayer;
            if (player == null || blocker != player)
            {
                return;
            }

            SyncOwner(player);
            int needed = GetNeeded(player);
            _inBlock = true;
            _guardBroke = false;
            _armed = needed > 0 && _charges >= needed;
        }

        // Called from SharedHumanoidBlockAttackPatch's postfix, whatever BlockAttack returned, so _inBlock always clears.
        public static void AfterBlock(Humanoid blocker, Character attacker, bool blocked)
        {
            Player player = Player.m_localPlayer;
            if (player == null || blocker != player || !_inBlock)
            {
                return;
            }

            _inBlock = false;

            // Vanilla's own success flag is HaveStamina() && !guard break. For a tower shield, stamina does not change
            // between that test and here: the only later stamina change is the parry branch, which towers never reach.
            bool held = blocked && !_guardBroke && player.HaveStamina();
            if (!held)
            {
                return;
            }

            if (_armed)
            {
                Release(player);
                return;
            }

            if (attacker == null)
            {
                return;
            }

            int needed = GetNeeded(player);
            if (needed <= 0 || _charges >= needed)
            {
                return;
            }

            _charges++;
            SaveCharges(player);
            PublishRocks(player);
        }

        // Records whether the block being processed broke the guard. BlockAttack passes a null hit; RPC_Damage's own
        // stagger passes the hit. Last, so a suppressed stagger (Immovable, Blood Stagger Block) reads as false.
        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        private static class Character_AddStaggerDamage_Patch
        {
            [HarmonyPriority(Priority.Last)]
            [HarmonyPostfix]
            private static void Postfix(Character __instance, HitData hit, bool __result)
            {
                if (_inBlock && hit == null && __result && __instance == Player.m_localPlayer)
                {
                    _guardBroke = true;
                }
            }
        }

        // A wave with nowhere to go (a wall right ahead, deep water, a sheer drop) keeps the charge for the next block.
        private static void Release(Player player)
        {
            ItemDrop.ItemData blocker = player.GetCurrentBlocker();
            Vector3 dir = player.transform.forward;
            dir.y = 0f;
            if (blocker == null || dir.sqrMagnitude < 0.0001f)
            {
                return;
            }
            dir.Normalize();

            Vector3 origin = player.transform.position;
            float spacing = StepSpacing;
            int maxSteps = Mathf.Max(1, Mathf.CeilToInt(Length / spacing));
            int steps = WalkSteps(origin, dir, maxSteps, spacing, MaxStepRise, StepPoints);
            if (steps <= 0)
            {
                EpicLoot.Log("[KineticQuake] No room for the wave; the charge is kept.");
                return;
            }

            _charges = 0;
            SaveCharges(player);
            PublishRocks(player);

            // Includes the Blocking skill and EpicLoot's block-power bonuses (the set's own 2-piece among them).
            float blockPower = blocker.GetBlockPower(player.GetSkillFactor(Skills.SkillType.Blocking));
            float damage = blockPower * DamagePercent / 100f;
            float interval = spacing / WaveSpeed;

            EpicLoot.Log($"[KineticQuake] Release: block power {blockPower:0.#}, {damage:0.#} blunt, {steps} of " +
                $"{maxSteps} steps.");

            ZNetView nview = player.m_nview;
            if (nview != null && nview.IsValid())
            {
                nview.InvokeRPC(ZNetView.Everybody, FxRpc, origin, dir, steps, spacing, interval, FxScale);
            }

            if (damage > 0f)
            {
                player.StartCoroutine(DamageWave(player, origin, dir, new List<Vector3>(StepPoints), interval, damage,
                    Width * 0.5f));
            }
        }

        // The wave's ground points, one every `spacing` metres along `dir`, each snapped to the ground below it. It stops
        // early at a wall between two points, or where the ground rises or drops more than maxRise between them. Every
        // client walks the same steps, so the eruptions it plays line up with the damage.
        private static int WalkSteps(Vector3 origin, Vector3 dir, int maxSteps, float spacing, float maxRise,
            List<Vector3> points)
        {
            points.Clear();
            Vector3 previous = origin;
            for (int i = 1; i <= maxSteps; i++)
            {
                Vector3 flat = origin + dir * (i * spacing);
                if (!TryFindGround(flat, previous.y, maxRise, out float groundY))
                {
                    break;
                }

                Vector3 point = new Vector3(flat.x, groundY, flat.z);
                Vector3 up = Vector3.up * WallCheckHeight;
                if (Physics.Linecast(previous + up, point + up, WallMask, QueryTriggerInteraction.Ignore))
                {
                    break;
                }

                points.Add(point);
                previous = point;
            }

            return points.Count;
        }

        // The highest surface below `point` within maxRise of the previous step. Taking the highest at or below the
        // limit, rather than the first hit, keeps a low ceiling or a roof from reading as a sudden rise.
        private static bool TryFindGround(Vector3 point, float previousY, float maxRise, out float groundY)
        {
            groundY = 0f;
            float top = previousY + maxRise + 0.5f;
            float distance = maxRise * 2f + 0.5f;
            int count = Physics.RaycastNonAlloc(new Vector3(point.x, top, point.z), Vector3.down, GroundHits, distance,
                GroundMask, QueryTriggerInteraction.Ignore);

            bool found = false;
            for (int i = 0; i < count; i++)
            {
                float y = GroundHits[i].point.y;
                if (y - previousY > maxRise || previousY - y > maxRise)
                {
                    continue;
                }

                if (!found || y > groundY)
                {
                    groundY = y;
                    found = true;
                }
            }

            return found;
        }

        // The damage, on the releasing client: each step hits the band between it and the step before, and every
        // character is hit at most once by the whole wave. Earthshaker's slam filters, as a bonus hit.
        private static IEnumerator DamageWave(Player player, Vector3 origin, Vector3 dir, List<Vector3> points,
            float interval, float damage, float radius)
        {
            var hitSet = new HashSet<GameObject>();
            Vector3 previous = origin;
            float stagger = StaggerMultiplier;
            float push = PushForce;
            Vector3 up = Vector3.up * HitHeight;
            int damaged = 0;

            // The first step waits a frame too: it would otherwise deal its damage inside the player's own blocked hit
            // (this postfix runs within RPC_Damage), on a frame that already carries the release.
            yield return null;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(interval);
                }

                if (player == null || player.IsDead())
                {
                    yield break;
                }

                Vector3 point = points[i];
                int count = Physics.OverlapCapsuleNonAlloc(previous + up, point + up, radius, HitBuffer, CharacterMask,
                    QueryTriggerInteraction.UseGlobal);
                for (int c = 0; c < count; c++)
                {
                    if (HitCharacter(player, HitBuffer[c], point + up, dir, damage, push, stagger, hitSet))
                    {
                        damaged++;
                    }
                }

                previous = point;
            }

            EpicLoot.Log($"[KineticQuake] Wave done: {damaged} damaged.");
        }

        private static bool HitCharacter(Player player, Collider collider, Vector3 center, Vector3 dir, float damage,
            float push, float stagger, HashSet<GameObject> hitSet)
        {
            if (collider == null)
            {
                return false;
            }

            GameObject target = Projectile.FindHitObject(collider);
            if (target == null || target == player.gameObject || !hitSet.Add(target))
            {
                return false;
            }

            Character character = target.GetComponent<Character>();
            if (character == null || character.IsDead())
            {
                return false;
            }

            bool isEnemy = BaseAI.IsEnemy(player, character) ||
                (character.GetBaseAI() != null && character.GetBaseAI().IsAggravatable());
            if (!player.IsPVPEnabled() && !isEnemy)
            {
                return false;
            }

            if (character.IsDodgeInvincible())
            {
                (character as Player)?.HitWhileDodging();
                return false;
            }

            Vector3 point = collider is MeshCollider ? collider.ClosestPointOnBounds(center) : collider.ClosestPoint(center);
            var hit = new HitData
            {
                m_point = point,
                m_dir = dir,
                m_hitCollider = collider,
                m_pushForce = push,
                m_staggerMultiplier = stagger,
                m_dodgeable = true,
                m_blockable = true,
                m_hitType = HitData.HitType.PlayerHit,
                m_skill = Skills.SkillType.None,
            };
            hit.m_damage.m_blunt = damage;
            hit.SetAttacker(player);
            HitSource.DealBonusDamage(character, hit);
            return true;
        }

        // Every client, the blocker included: the same step walk, an eruption at each step in turn, one stomp sound and
        // one camera shake. The eruptions' own shakes are removed, or six of them would stack.
        private static void OnFxRpc(Player player, Vector3 origin, Vector3 dir, int steps, float spacing, float interval,
            float fxScale)
        {
            if (ZNet.instance != null && ZNet.instance.IsDedicated())
            {
                return;
            }

            int count = WalkSteps(origin, dir, Mathf.Clamp(steps, 0, 100), spacing, MaxStepRise, FxStepPoints);
            if (count <= 0 || player == null)
            {
                return;
            }

            GameObject sound = FindFxPrefab(EruptionSfxPrefab, logMissing: true);
            if (sound != null)
            {
                LocalFx.Spawn(sound, origin, Quaternion.identity, lifetime: LocalFxLifetime);
            }

            if (GameCamera.instance != null)
            {
                GameCamera.instance.AddShake(origin, ShakeRange, ShakeStrength, false);
            }

            player.StartCoroutine(PlayEruptions(new List<Vector3>(FxStepPoints), Quaternion.LookRotation(dir), interval,
                fxScale));
        }

        private static IEnumerator PlayEruptions(List<Vector3> points, Quaternion rotation, float interval, float scale)
        {
            GameObject prefab = null;
            foreach (string name in EruptionFxPrefabs)
            {
                prefab = FindFxPrefab(name, logMissing: false);
                if (prefab != null)
                {
                    break;
                }
            }

            if (prefab == null)
            {
                if (MissingFxLogged.Add(EruptionFxPrefabs[0]))
                {
                    EpicLoot.LogWarning($"[KineticQuake] could not find the '{EruptionFxPrefabs[0]}' prefab; the wave " +
                        "plays without eruptions.");
                }
                yield break;
            }

            // From the next frame, like the damage: the release frame already carries the block, the sound and the shake.
            yield return null;
            for (int i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    yield return new WaitForSeconds(interval);
                }

                LocalFx.Spawn(prefab, points[i], rotation, scale, lifetime: LocalFxLifetime, noCameraShake: true);
            }
        }

        // World load (SetEffectWarmup): the copies the wave and the rocks spawn from.
        internal static void WarmupFx()
        {
            foreach (string name in EruptionFxPrefabs)
            {
                GameObject eruption = FindFxPrefab(name, logMissing: false);
                if (eruption != null)
                {
                    LocalFx.Prewarm(eruption, scaled: !Mathf.Approximately(FxScale, 1f), noCameraShake: true);
                    break;
                }
            }

            LocalFx.Prewarm(FindFxPrefab(EruptionSfxPrefab, logMissing: false), scaled: false);
            LocalFx.Prewarm(FindFxPrefab(RockPuffFxPrefab, logMissing: false), scaled: true);
        }

        internal static void SpawnRockPuff(Vector3 position)
        {
            GameObject prefab = FindFxPrefab(RockPuffFxPrefab, logMissing: true);
            if (prefab != null)
            {
                LocalFx.Spawn(prefab, position, Quaternion.identity, RockPuffScale, lifetime: LocalFxLifetime);
            }
        }

        private static GameObject FindFxPrefab(string name, bool logMissing)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && logMissing && MissingFxLogged.Add(name))
            {
                EpicLoot.LogWarning($"[KineticQuake] could not find the '{name}' prefab; playing without it.");
            }
            return prefab;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    PublishRocks(__instance);
                }
            }
        }

        // Every player, local or remote, plays the wave and carries the rocks; a headless server draws nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                Player player = __instance;
                player.m_nview?.Register<Vector3, Vector3, int, float, float, float>(FxRpc,
                    (sender, origin, dir, steps, spacing, interval, fxScale) =>
                        OnFxRpc(player, origin, dir, steps, spacing, interval, fxScale));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    player.gameObject.AddComponent<KineticQuakeRocks>();
                }
            }
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
        private static class Hud_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Hud __instance) => KineticQuakeHud.Refresh(__instance);
        }
    }

    // The charge as 1-5 small rocks circling the player at the waist, on every client, from the count the owner mirrors
    // to the player ZDO. They speed up once the quake is armed and break apart when it is released. Each rock is a bare
    // mesh copied from the vanilla Stone item (no collider, no ZNetView), not parented to the player, so it doesn't
    // lean or tumble with the body.
    public class KineticQuakeRocks : MonoBehaviour
    {
        private const string RockItemPrefab = "Stone";
        private const float RockScale = 0.6f;          // of the Stone item's own model
        private const float OrbitRadius = 0.9f;
        private const float OrbitHeight = 1f;
        private const float OrbitSpeed = 90f;          // degrees per second
        private const float ArmedOrbitSpeed = 180f;
        private const float SpreadSpeed = 180f;        // degrees per second a rock moves to its new place in the ring
        private const float BobHeight = 0.1f;
        private const float BobRate = 2f;              // radians per second
        private const float TumbleSpeed = 60f;         // degrees per second
        private const float GrowTime = 0.25f;

        private class Rock
        {
            public GameObject Object;
            public float Slot;                         // degrees around the ring, eased toward its even spacing
            public float Grow;
            public float Phase;
            public Vector3 Axis;
        }

        private static Mesh _mesh;
        private static Material[] _materials;
        private static Vector3 _baseScale;
        private static bool _assetsFailed;

        private readonly List<Rock> _rocks = new List<Rock>();
        private Player _player;
        private float _orbitAngle;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void OnDestroy()
        {
            foreach (Rock rock in _rocks)
            {
                if (rock.Object != null)
                {
                    Destroy(rock.Object);
                }
            }
            _rocks.Clear();
        }

        // The main-menu preview player has no ZDO, so it never shows rocks.
        private int GetTargetCount()
        {
            if (_player == null || _player.IsDead())
            {
                return 0;
            }

            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            return zdo == null ? 0 : Mathf.Clamp(zdo.GetInt(KineticQuake.RocksZdoHash), 0, KineticQuake.MaxRocks);
        }

        private void LateUpdate()
        {
            int target = GetTargetCount();
            if (target > 0 && !EnsureAssets())
            {
                target = 0;
            }

            // Rocks break with a puff only on a release. A player leaving (out of range, logged out, through a portal) has
            // lost its ZDO by now, and Player.IsDead reads false without one, so that is checked first. The puffs are
            // one rock a frame rather than all five on the release frame.
            bool puff = _player != null && _player.m_nview != null && _player.m_nview.IsValid() && !_player.IsDead();
            if (puff && _rocks.Count > target)
            {
                RemoveRock(_rocks.Count - 1, true);
            }
            else
            {
                while (_rocks.Count > target)
                {
                    RemoveRock(_rocks.Count - 1, false);
                }
            }

            while (_rocks.Count < target)
            {
                AddRock();
            }

            if (_rocks.Count == 0)
            {
                return;
            }

            float dt = Time.deltaTime;
            _orbitAngle = Mathf.Repeat(_orbitAngle + (target >= KineticQuake.MaxRocks ? ArmedOrbitSpeed : OrbitSpeed) * dt,
                360f);

            Vector3 center = _player.transform.position + Vector3.up * OrbitHeight;
            float spacing = 360f / _rocks.Count;
            for (int i = 0; i < _rocks.Count; i++)
            {
                Rock rock = _rocks[i];
                if (rock.Object == null)
                {
                    continue;
                }

                rock.Slot = Mathf.MoveTowardsAngle(rock.Slot, i * spacing, SpreadSpeed * dt);
                rock.Grow = Mathf.Min(1f, rock.Grow + dt / GrowTime);

                float angle = (_orbitAngle + rock.Slot) * Mathf.Deg2Rad;
                float bob = Mathf.Sin(Time.time * BobRate + rock.Phase) * BobHeight;
                Transform t = rock.Object.transform;
                t.position = center + new Vector3(Mathf.Cos(angle) * OrbitRadius, bob, Mathf.Sin(angle) * OrbitRadius);
                t.rotation = Quaternion.AngleAxis(TumbleSpeed * dt, rock.Axis) * t.rotation;
                t.localScale = _baseScale * (RockScale * Mathf.SmoothStep(0f, 1f, rock.Grow));
            }
        }

        private void AddRock()
        {
            var go = new GameObject("EL_KineticQuakeRock");
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = _materials;
            go.transform.rotation = Random.rotationUniform;
            go.transform.localScale = Vector3.zero;

            // A new rock starts where the ring's last one is, and slides into its place as the ring re-spaces.
            float slot = _rocks.Count > 0 ? _rocks[_rocks.Count - 1].Slot : 0f;
            _rocks.Add(new Rock
            {
                Object = go,
                Slot = slot,
                Grow = 0f,
                Phase = Random.Range(0f, Mathf.PI * 2f),
                Axis = Random.onUnitSphere,
            });
        }

        private void RemoveRock(int index, bool puff)
        {
            Rock rock = _rocks[index];
            _rocks.RemoveAt(index);
            if (rock.Object == null)
            {
                return;
            }

            if (puff)
            {
                KineticQuake.SpawnRockPuff(rock.Object.transform.position);
            }
            Destroy(rock.Object);
        }

        // Looked up once, from ObjectDB; a failure logs once and leaves the rocks off for the session.
        private static bool EnsureAssets()
        {
            if (_mesh != null)
            {
                return true;
            }

            if (_assetsFailed || ObjectDB.instance == null)
            {
                return false;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(RockItemPrefab);
            MeshFilter filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>(true) : null;
            MeshRenderer renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            if (filter == null || filter.sharedMesh == null || renderer == null)
            {
                _assetsFailed = true;
                EpicLoot.LogWarning($"[KineticQuake] could not find the '{RockItemPrefab}' item's model; the charge " +
                    "shows without orbiting rocks.");
                return false;
            }

            _mesh = filter.sharedMesh;
            _materials = renderer.sharedMaterials;
            _baseScale = filter.transform.lossyScale;
            return true;
        }
    }

    // The charge bar: a clone of vanilla's adrenaline bar, recoloured amber, under the stamina bar, one row below the
    // adrenaline bar when that is showing. It keeps the bar's Animator, so it shows, hides and flashes as vanilla's
    // does: shown while the local player wears the full set, flashing when the quake is armed, then pulsing.
    internal static class KineticQuakeHud
    {
        private const float BarWidth = 160f;           // pixels; vanilla's bar is 64 per 25 max adrenaline
        private const float RowOffset = 20f;           // pixels below vanilla's adrenaline bar when that is showing
        private const float BaseY = 130f;              // vanilla's adrenaline/stamina bar position
        private const float RaisedBaseY = 320f;        // ... with the build or ship HUD up
        private const float PulseRate = 4f;            // radians per second

        private static readonly Color ChargeColor = new Color(0.78f, 0.55f, 0.25f);
        private static readonly Color ArmedColor = new Color(1f, 0.8f, 0.35f);
        private static readonly Color SlowColor = new Color(0.45f, 0.3f, 0.12f);
        private static readonly int VisibleParam = Animator.StringToHash("Visible");
        private static readonly int FlashParam = Animator.StringToHash("Flash");

        private static Hud _hud;
        private static RectTransform _root;
        private static Animator _animator;
        private static GuiBar _fast;
        private static GuiBar _slow;
        private static TMP_Text _text;
        private static bool _wasArmed;
        private static int _shownCharges = -1;
        private static int _shownNeeded = -1;

        internal static void Refresh(Hud hud)
        {
            if (_hud != hud)
            {
                _hud = hud;
                Build(hud);
            }

            if (_root == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            int charges = 0;
            int needed = 0;
            bool show = player != null && !player.IsDead() &&
                KineticQuake.TryGetLocalState(player, out charges, out needed);
            if (_animator != null)
            {
                _animator.SetBool(VisibleParam, show);
            }

            if (!show)
            {
                _wasArmed = false;
                return;
            }

            bool armed = charges >= needed;
            if (armed && !_wasArmed && _animator != null)
            {
                _animator.SetTrigger(FlashParam);
            }
            _wasArmed = armed;

            bool raised = (hud.m_buildHud != null && hud.m_buildHud.activeSelf) ||
                (hud.m_shipHudRoot != null && hud.m_shipHudRoot.activeSelf);
            float y = raised ? RaisedBaseY : BaseY;
            if (player.GetAdrenaline() > 0f)
            {
                y -= RowOffset;
            }
            _root.anchoredPosition = new Vector2(0f, y);
            _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, BarWidth + hud.m_staminaBarBorderBuffer);

            float value = charges / (float)needed;
            Color color = ChargeColor;
            if (armed)
            {
                color = Color.Lerp(ChargeColor, ArmedColor, 0.5f + 0.5f * Mathf.Sin(Time.time * PulseRate));
            }

            if (_fast != null)
            {
                _fast.SetWidth(BarWidth);
                _fast.SetValue(value);
                _fast.SetColor(color);
            }

            if (_slow != null)
            {
                _slow.SetWidth(BarWidth);
                _slow.SetValue(value);
                _slow.SetColor(SlowColor);
            }

            if (_text != null && (charges != _shownCharges || needed != _shownNeeded))
            {
                _shownCharges = charges;
                _shownNeeded = needed;
                _text.text = $"{charges}/{needed}";
            }
        }

        private static void Build(Hud hud)
        {
            _root = null;
            _animator = null;
            _fast = null;
            _slow = null;
            _text = null;
            _wasArmed = false;
            _shownCharges = -1;
            _shownNeeded = -1;

            RectTransform source = hud != null ? hud.m_adrenalineBarRoot : null;
            if (source == null)
            {
                return;
            }

            GameObject clone = Object.Instantiate(source.gameObject, source.parent, false);
            clone.name = "EL_KineticQuakeBar";
            clone.SetActive(true);

            _root = (RectTransform)clone.transform;
            _animator = clone.GetComponent<Animator>();
            _fast = FindCounterpart<GuiBar>(source, _root, hud.m_adrenalineBarFast);
            _slow = FindCounterpart<GuiBar>(source, _root, hud.m_adrenalineBarSlow);
            _text = FindCounterpart<TMP_Text>(source, _root, hud.m_adrenalineText);
            if (_animator != null)
            {
                _animator.SetBool(VisibleParam, false);
            }
        }

        // The clone's copy of a component under the original root, found by its path from that root.
        private static T FindCounterpart<T>(Transform sourceRoot, Transform cloneRoot, Component source) where T : Component
        {
            if (source == null)
            {
                return null;
            }

            var path = new List<string>();
            Transform current = source.transform;
            while (current != null && current != sourceRoot)
            {
                path.Add(current.name);
                current = current.parent;
            }

            if (current == null)
            {
                return null;
            }

            Transform found = cloneRoot;
            for (int i = path.Count - 1; i >= 0 && found != null; i--)
            {
                found = found.Find(path[i]);
            }

            return found != null ? found.GetComponent<T>() : null;
        }
    }
}
