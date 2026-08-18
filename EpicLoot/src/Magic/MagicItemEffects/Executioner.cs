using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public class ExecutionerProjectileHit_Projectile_OnHit_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Projectile __instance)
        {
            if (__instance != null && __instance.m_nview != null && __instance.m_nview.GetZDO() is ZDO zdo)
            {
                ExecutionerCheckDamage_Character_Damage_Patch.ExecutionerMultiplier = zdo.GetFloat("epic loot executioner multiplier", 1f);
            }
        }

        [UsedImplicitly]
        private static void Postfix() => ExecutionerCheckDamage_Character_Damage_Patch.ExecutionerMultiplier = null;
    }

    public class ExecutionerCheckDamage_Character_Damage_Patch
    {
        public static float? ExecutionerMultiplier;

        // Targets the local player has already executed. Tracked attacker-side (this runs on the attacker's
        // client) so the once-per-target dedupe doesn't depend on writing a ZDO we may not own -- a non-owner
        // ZDO write isn't authoritative and gets reverted on the owner's next sync. ZDOIDs are unique per
        // spawn, so a respawned enemy gets a fresh id and can be executed again.
        private static readonly HashSet<ZDOID> _executedTargets = new HashSet<ZDOID>();

        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier, runs at
        // Priority.Last so the execute multiplier lands after other damage modifiers).
        public static void ModifyOutgoingHit(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null || hit.GetAttacker() != Player.m_localPlayer)
            {
                ExecutionerMultiplier = null;
                return;
            }

            var player = Player.m_localPlayer;
            var targetId = __instance.GetZDOID();
            if (!targetId.IsNone() && _executedTargets.Contains(targetId))
            {
                return;
            }

            if (ExecutionerMultiplier == null)
            {
                ExecutionerMultiplier = ReadExecutionerValue(player);
            }

            if (ExecutionerMultiplier is float multiplier && __instance.GetHealth() / __instance.GetMaxHealth() < 0.2f)
            {
                hit.m_damage.Modify(multiplier);
                if (!targetId.IsNone())
                {
                    // Bounded so a long session can't grow this without limit; executed targets almost always
                    // die on the burst, so this rarely matters and a clear just re-allows a survivor's execute.
                    if (_executedTargets.Count > 1024)
                    {
                        _executedTargets.Clear();
                    }
                    _executedTargets.Add(targetId);
                }
            }

            ExecutionerMultiplier = null;
        }

        public static float ReadExecutionerValue(Player player)
        {
            float totalMagicEffect;
            if (Attack_Patch.ActiveAttack != null)
                totalMagicEffect = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, Attack_Patch.ActiveAttack.m_weapon, MagicEffectType.Executioner, 0.01f);
            else
                totalMagicEffect = player.GetTotalActiveMagicEffectValue(MagicEffectType.Executioner, 0.01f);
            return 1 + totalMagicEffect;
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public class ExecutionerProjectileInstantiation_Attack_FireProjectileBurst_Patch
    {
        private static GameObject MarkAttackProjectile(GameObject attackProjectile, Attack attack)
        {
            if (attack != null && attackProjectile != null && attack.m_character == Player.m_localPlayer)
            {
                var znetView = attackProjectile.GetComponent<ZNetView>();
                if (znetView != null && znetView.GetZDO() != null)
                {
                    znetView.GetZDO().Set("epic loot executioner multiplier", ExecutionerCheckDamage_Character_Damage_Patch.ReadExecutionerValue(Player.m_localPlayer));
                }
            }

            return attackProjectile;
        }

        private static readonly MethodInfo AttackProjectileMarker = AccessTools.DeclaredMethod(typeof(ExecutionerProjectileInstantiation_Attack_FireProjectileBurst_Patch), nameof(MarkAttackProjectile));
        private static readonly MethodInfo Instantiator = AccessTools.GetDeclaredMethods(typeof(Object)).Where(m => m.Name == "Instantiate" && m.GetGenericArguments().Length == 1).Select(m => m.MakeGenericMethod(typeof(GameObject))).First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

        [UsedImplicitly]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.opcode == OpCodes.Call && instruction.OperandIs(Instantiator))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                    yield return new CodeInstruction(OpCodes.Call, AttackProjectileMarker);
                }
            }
        }
    }
}