using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Artillery = nameof(Artillery);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Artillery (set items only, NoRoll). The wearer's crossbow shots leave as lobbed fireballs: the Staff of
    // Embers projectile, arced to come down on the point the player aims at, dealing the shot's damage +value%
    // to everything within Radius metres of the impact. The rarity raises the damage; the radius is the same at
    // every rarity.
    //
    // The fireball is still the crossbow's shot: the bolt is spent, its hit data (bolt and crossbow damage, skill,
    // status effects) is the one vanilla built, and the blast is a weapon strike, so on-hit enchantments proc on
    // every target it touches. Only the projectile's owner simulates it (Projectile.FixedUpdate), so the arc,
    // gravity, blast radius and damage set on this client's instance are all there is to it; other clients see
    // the networked fireball fly.
    //
    // A triple shot (TripleBowShot) would put all three fireballs on one spot, since a crossbow has no spread, so
    // each of its fireballs comes down at a random point within TripleShotSpread x range of the aim point instead.
    public static class Artillery
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMaxDistance = 60f;       // metres; aiming further comes down at this range
        public const float DefaultGravity = 30f;           // metres/second^2 pulling the fireball down
        public const float DefaultArcHeight = 0.2f;        // apex height above the higher end, per metre of range
        public const float DefaultRadius = 2.5f;           // blast radius in metres
        public const float DefaultTripleShotSpread = 0.15f; // a triple shot's scatter, as a fraction of the range

        private const string MaxDistanceKey = "MaxDistance";
        private const string GravityKey = "Gravity";
        private const string ArcHeightKey = "ArcHeight";
        private const string RadiusKey = "Radius";
        private const string TripleShotSpreadKey = "TripleShotSpread";

        private const string FireballPrefab = "staff_fireball_projectile";
        private const float MinArcHeight = 1f;       // a point-blank shot still rises this far
        private const float CeilingClearance = 0.5f; // the apex stays this far below a roof over the shooter

        // Player-editable, so each gets a floor: a zero gravity divides, a negative range or radius is meaningless.
        private static float MaxDistance => EffectConfig.GetClamped(MagicEffectType.Artillery, MaxDistanceKey, DefaultMaxDistance, 5f, 300f);
        private static float Gravity => Mathf.Max(1f, EffectConfig.Get(MagicEffectType.Artillery, GravityKey, DefaultGravity));
        private static float ArcHeight => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.Artillery, ArcHeightKey, DefaultArcHeight));
        private static float Radius => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.Artillery, RadiusKey, DefaultRadius));
        private static float TripleShotSpread => EffectConfig.GetClamped(MagicEffectType.Artillery, TripleShotSpreadKey, DefaultTripleShotSpread, 0f, 1f);

        // Tooltip: "... +{0}% damage ... within {1}m" -- {1} follows a retune of Radius.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Artillery, value => new object[] { value, GetRadius() });
        }

        // Rounded to the centimetre so the tooltip shows the radius the blast actually uses.
        private static float GetRadius()
        {
            return Mathf.Round(Radius * 100f) / 100f;
        }

        // The fireball FireProjectileBurst just instantiated in place of a bolt, until its Setup call.
        private static GameObject _pendingShot;
        private static float _pendingValue;
        private static bool _missingPrefabLogged;

        private static int _aimMask;
        private static int _ceilingMask;
        private static readonly RaycastHit[] RayHits = new RaycastHit[32];

        // Vanilla's projectile mask (Projectile.s_rayMaskSolids), so the aim ray stops on what the fireball would.
        private static int AimMask => _aimMask != 0 ? _aimMask : _aimMask = LayerMask.GetMask("Default", "static_solid",
            "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost",
            "hitbox", "character_noenv", "vehicle");

        // Only what makes a roof; a creature flying overhead should not flatten the arc.
        private static int CeilingMask => _ceilingMask != 0 ? _ceilingMask : _ceilingMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain", "vehicle");

        // Replaces the Object.Instantiate call in Attack.FireProjectileBurst: the fireball for an Artillery
        // crossbow shot by the local player, the vanilla projectile otherwise.
        private static GameObject SpawnProjectile(GameObject prefab, Vector3 position, Quaternion rotation, Attack attack)
        {
            _pendingShot = null;
            var fireball = GetFireball(attack, out float value);
            if (fireball == null)
            {
                return Object.Instantiate(prefab, position, rotation);
            }

            var shot = Object.Instantiate(fireball, position, rotation);
            _pendingShot = shot;
            _pendingValue = value;
            return shot;
        }

        private static GameObject GetFireball(Attack attack, out float value)
        {
            value = 0f;
            if (attack == null || attack.m_character == null || attack.m_character != Player.m_localPlayer ||
                attack.m_weapon == null || attack.m_weapon.m_shared.m_skillType != Skills.SkillType.Crossbows ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.Artillery, out value))
            {
                return null;
            }

            var fireball = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(FireballPrefab) : null;
            if (fireball == null && !_missingPrefabLogged)
            {
                _missingPrefabLogged = true;
                EpicLoot.LogWarning($"Artillery: could not find the '{FireballPrefab}' prefab; crossbow shots stay bolts.");
            }
            return fireball;
        }

        // Runs after vanilla's Setup gave the fireball the shot's hit data and a straight-line velocity.
        private static void Launch(Projectile projectile, Vector3 firedVelocity, float value)
        {
            if (projectile.m_owner is not Player player)
            {
                return;
            }

            Vector3 from = projectile.transform.position;
            float gravity = Gravity;
            Vector3 target = FindTarget(player);
            if (MultiShot.IsTripleShotFiring)
            {
                target = Scatter(from, target);
            }
            Vector3 velocity = SolveArc(from, target, gravity);

            // Keep vanilla's sideways spread (accuracy, several projectiles per shot) by turning the arc as far as
            // the straight shot was turned away from the aim.
            Vector3 aim = player.GetLookDir();
            aim.y = 0f;
            firedVelocity.y = 0f;
            if (aim.sqrMagnitude > 0.0001f && firedVelocity.sqrMagnitude > 0.0001f)
            {
                velocity = Quaternion.AngleAxis(Vector3.SignedAngle(aim, firedVelocity, Vector3.up), Vector3.up) * velocity;
            }

            projectile.m_vel = velocity;
            projectile.m_gravity = gravity;
            projectile.m_aoe = GetRadius();
            projectile.m_damage.Modify(1f + value / 100f);
            projectile.transform.rotation = Quaternion.LookRotation(velocity);
        }

        // The first thing along the look direction within range, else the ground under the end of that range.
        private static Vector3 FindTarget(Player player)
        {
            float maxDistance = MaxDistance;
            Vector3 from = player.GetEyePoint();
            Vector3 dir = player.GetLookDir();
            int count = Physics.RaycastNonAlloc(from, dir, RayHits, maxDistance, AimMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            Vector3 target = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var hit = RayHits[i];
                if (hit.distance >= nearest || Projectile.FindHitObject(hit.collider) == player.gameObject)
                {
                    continue;
                }

                nearest = hit.distance;
                target = hit.point;
            }

            if (nearest < float.MaxValue)
            {
                return target;
            }

            Vector3 end = from + dir * maxDistance;
            if (Physics.Raycast(end + Vector3.up, Vector3.down, out var ground, 500f, AimMask, QueryTriggerInteraction.Ignore))
            {
                return ground.point;
            }

            if (ZoneSystem.instance != null)
            {
                end.y = ZoneSystem.instance.GetGroundHeight(end);
            }
            return end;
        }

        // A random point within TripleShotSpread x range of the target, level with it.
        private static Vector3 Scatter(Vector3 from, Vector3 target)
        {
            Vector3 flat = target - from;
            flat.y = 0f;
            Vector2 offset = Random.insideUnitCircle * (flat.magnitude * TripleShotSpread);
            return target + new Vector3(offset.x, 0f, offset.y);
        }

        // The launch velocity of a parabola from `from` to `to` under `gravity`, peaking ArcHeight x range above
        // the higher end (at least MinArcHeight), and lower when a roof over the shooter would be in the way.
        private static Vector3 SolveArc(Vector3 from, Vector3 to, float gravity)
        {
            Vector3 flat = to - from;
            flat.y = 0f;
            float top = Mathf.Max(from.y, to.y);
            float apex = top + Mathf.Max(MinArcHeight, flat.magnitude * ArcHeight);

            if (Physics.Raycast(from, Vector3.up, out var roof, apex - from.y + CeilingClearance, CeilingMask,
                QueryTriggerInteraction.Ignore))
            {
                apex = Mathf.Max(roof.point.y - CeilingClearance, top + 0.1f);
            }

            // Rise to the apex, fall from it to the target; the horizontal speed covers the range in that time.
            float rise = Mathf.Sqrt(2f * gravity * (apex - from.y));
            float time = rise / gravity + Mathf.Sqrt(2f * (apex - to.y) / gravity);
            return flat / time + Vector3.up * rise;
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        private static class Attack_FireProjectileBurst_Patch
        {
            private static readonly MethodInfo Instantiator = AccessTools.GetDeclaredMethods(typeof(Object))
                .Where(m => m.Name == nameof(Object.Instantiate) && m.GetGenericArguments().Length == 1)
                .Select(m => m.MakeGenericMethod(typeof(GameObject)))
                .First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

            private static readonly MethodInfo Spawner = AccessTools.DeclaredMethod(typeof(Artillery), nameof(SpawnProjectile));

            // Priority.Last: Executioner and ModifyStaggerDuration insert their ZDO markers right after this same
            // Instantiate call. Applied after theirs, the call is swapped in place and the markers they added
            // after it land on the fireball.
            [HarmonyTranspiler]
            [HarmonyPriority(Priority.Last)]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                bool replaced = false;
                foreach (var instruction in instructions)
                {
                    if (!replaced && instruction.opcode == OpCodes.Call && instruction.OperandIs(Instantiator))
                    {
                        replaced = true;
                        yield return new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(instruction); // this
                        yield return new CodeInstruction(OpCodes.Call, Spawner);
                        continue;
                    }

                    yield return instruction;
                }

                if (!replaced)
                {
                    EpicLoot.LogWarning("Artillery: could not find the projectile spawn in Attack.FireProjectileBurst; " +
                        "crossbow shots stay bolts.");
                }
            }
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Projectile_Setup_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, Vector3 velocity)
            {
                if (_pendingShot == null || __instance.gameObject != _pendingShot)
                {
                    return;
                }

                _pendingShot = null;
                Launch(__instance, velocity, _pendingValue);
            }
        }
    }
}
