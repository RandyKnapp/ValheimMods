using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string PiercingBolts = nameof(PiercingBolts);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // PiercingBolts (set items only, NoRoll). The wearer's crossbow bolts fly on through every creature in their
    // path, striking each once, and keep value% of their damage after each one. Terrain, buildings, trees and
    // rocks still stop them as usual. Reworked from RustyLoot's PiercingShot, which spawned a fresh bolt behind
    // the first target instead (one extra hit at most).
    //
    // Vanilla stops a bolt on its first creature: Projectile.OnHit sets m_didHit and, the creature's collider
    // having a rigidbody and bolts having m_stayAfterHitDynamic off, destroys it. The pass-through reuses the
    // switches vanilla's own pass-through projectile (the Orb of Ahri staff) is built on: with
    // m_onlyStopOnTerrain set, OnHit deals its damage, effects, skill raise and adrenaline but neither stops nor
    // (with m_stayAfterHitDynamic set) destroys the projectile. They are set only for the length of a creature
    // hit, so everything else still stops the bolt the vanilla way.
    //
    // Once per creature: each fixed step's ray starts half a step behind where the last one ended, and a
    // creature is often several colliders, so the same creature comes back many times while the bolt passes
    // through it. Vanilla's own m_hitList is per collider, which is not enough, so the creatures already struck
    // are kept per bolt (PierceState) and filtered at FixedUpdate's OnHit call sites, before OnHit -- and every
    // EpicLoot prefix on it -- runs again.
    //
    // Only the projectile's owner simulates it, so the shooter's client does all of this; other clients see the
    // bolt keep flying because no RPC_OnHit is sent for a pass-through.
    public static class PiercingBolts
    {
        private static readonly MethodInfo OnHitMethod = AccessTools.DeclaredMethod(typeof(Projectile), nameof(Projectile.OnHit));
        private static readonly MethodInfo OnHitInFlightMethod = AccessTools.DeclaredMethod(typeof(PiercingBolts), nameof(OnHitInFlight));

        // Per-bolt state, added in Projectile.Setup to bolts that pierce; it goes when the bolt is destroyed.
        private sealed class PierceState : MonoBehaviour
        {
            public readonly HashSet<Character> Struck = new HashSet<Character>();
            public float Keep;
        }

        // Replaces Projectile.OnHit at the FixedUpdate call sites. Anything that is not a piercing bolt meeting a
        // creature goes straight to vanilla.
        private static void OnHitInFlight(Projectile projectile, Collider collider, Vector3 hitPoint, bool water, Vector3 normal)
        {
            if (water || collider == null || !projectile.TryGetComponent(out PierceState state))
            {
                projectile.OnHit(collider, hitPoint, water, normal);
                return;
            }

            GameObject hitObject = Projectile.FindHitObject(collider);
            Character character = hitObject != null ? hitObject.GetComponent<Character>() : null;
            if (character == null)
            {
                projectile.OnHit(collider, hitPoint, water, normal);
                return;
            }

            // Vanilla returns from OnHit for an invalid target (the shooter, a tame, a dodging or non-PvP player)
            // without stopping the bolt either; skipping the call keeps its dodge bookkeeping to one run.
            if (state.Struck.Contains(character) || character.IsDead() || !projectile.IsValidTarget(character))
            {
                return;
            }

            state.Struck.Add(character);

            bool onlyStopOnTerrain = projectile.m_onlyStopOnTerrain;
            bool stayAfterHitDynamic = projectile.m_stayAfterHitDynamic;
            projectile.m_onlyStopOnTerrain = true;
            projectile.m_stayAfterHitDynamic = true;
            try
            {
                projectile.OnHit(collider, hitPoint, water, normal);
            }
            finally
            {
                projectile.m_onlyStopOnTerrain = onlyStopOnTerrain;
                projectile.m_stayAfterHitDynamic = stayAfterHitDynamic;
            }

            // The next creature is struck with what is left. OnHit builds each hit from these two fields.
            projectile.m_damage.Modify(state.Keep);
            projectile.m_attackForce *= state.Keep;
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Projectile_Setup_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, Character owner, ItemDrop.ItemData item)
            {
                // m_aoe is zero on every vanilla bolt; anything with a blast is not a bolt to pierce with.
                if (owner == null || owner != Player.m_localPlayer || item == null ||
                    item.m_shared.m_skillType != Skills.SkillType.Crossbows ||
                    (__instance.m_type & ProjectileType.Bolt) == 0 || __instance.m_aoe > 0f ||
                    !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.PiercingBolts, out float value))
                {
                    return;
                }

                var state = __instance.gameObject.AddComponent<PierceState>();
                state.Keep = Mathf.Clamp01(value / 100f);
            }
        }

        [HarmonyPatch(typeof(Projectile), "FixedUpdate")]
        private static class Projectile_FixedUpdate_Patch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                int replaced = 0;
                foreach (var instruction in instructions)
                {
                    if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                        instruction.OperandIs(OnHitMethod))
                    {
                        replaced++;
                        yield return new CodeInstruction(OpCodes.Call, OnHitInFlightMethod).MoveLabelsFrom(instruction)
                            .MoveBlocksFrom(instruction);
                        continue;
                    }

                    yield return instruction;
                }

                if (replaced == 0)
                {
                    EpicLoot.LogWarning("PiercingBolts: could not find Projectile.OnHit in Projectile.FixedUpdate; " +
                        "crossbow bolts will not pierce.");
                }
            }
        }
    }
}
