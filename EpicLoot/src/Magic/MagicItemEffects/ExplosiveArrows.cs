using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Attack))]
    public static class ExplodingArrow_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(Attack.FireProjectileBurst))]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Attack), nameof(Attack.m_weapon))),
                new CodeMatch(OpCodes.Ldloc_S),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_lastProjectile))))
                .ThrowIfNotMatch("Unable to patch FireProjectileBurst for Exploding Arrows.")
                .Advance(3).InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_S, (byte)20),
                new CodeInstruction(OpCodes.Ldarg_0),
                Transpilers.EmitDelegate(UpdateProjectileHit));
            return codeMatcher.Instructions();
        }

        private static void UpdateProjectileHit(GameObject shot, Attack instance)
        {
            if (Player.m_localPlayer != null && instance.m_character == Player.m_localPlayer &&
                Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ExplosiveArrows, out float effectValue, 0.01f))
            {
                Projectile projectile = shot.GetComponent<Projectile>();
                if (projectile != null && projectile.m_nview != null && projectile.m_nview.IsValid())
                {
                    projectile.m_nview.GetZDO().Set("el-aw", effectValue);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
    public static class ExplodingArrowHit_Projectile_OnHit_Patch
    {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher codeMatcher = new CodeMatcher(instructions);
            codeMatcher.MatchStartForward(
                    new CodeMatch(OpCodes.Ldarg_0), // Projectile instance
                    new CodeMatch(OpCodes.Ldc_I4_1),
                    new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(Projectile), nameof(Projectile.m_didHit))))
                .ThrowIfNotMatch("Unable to patch OnHit for Exploding Arrows.")
                .Advance(3)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_2), // Vector3 hitPoint
                    new CodeInstruction(OpCodes.Ldarg_0), // Projectile instance
                    Transpilers.EmitDelegate(SpawnExplosiveArrowOnHit));
            return codeMatcher.Instructions();
        }

        private static void SpawnExplosiveArrowOnHit(Vector3 hitPoint, Projectile instance)
        {
            if (instance.m_didHit)
            {
                float explodingArrowValue = instance.m_nview.GetZDO().GetFloat("el-aw", float.NaN);

                if (float.IsNaN(explodingArrowValue))
                {
                    return;
                }

                // The explosion needs the shooter as its owner: every one of Aoe.ShouldHit's filters (the
                // prefab's m_hitOwner / m_hitFriendly off) only applies when there is an owner, and the hits
                // carry it as their attacker, which is what vanilla's PvP check in RPC_Damage keys on. An
                // ownerless explosion hit every character in range -- players without PvP, tames and the
                // shooter included. With no live shooter to own it, there is no explosion.
                if (instance.m_owner == null)
                {
                    return;
                }

                GameObject prefab = PrefabManager.Instance.GetPrefab(EpicAssets.ExplosiveArrow);

                if (prefab == null)
                {
                    EpicLoot.LogError("Cannot find Explosive Arrow prefab! Magic Effect will not work as expected.");
                    return;
                }

                GameObject spawnedObject = GameObject.Instantiate(prefab, hitPoint, Quaternion.identity);

                Aoe aoe = spawnedObject.GetComponent<Aoe>();

                if (aoe == null)
                {
                    EpicLoot.LogError("Cannot find Explosive Arrow Aoe! Magic Effect will not work as expected.");
                    return;
                }

                // Set directly rather than through Aoe.Setup, which would also apply the weapon's upgrade and
                // world-level bonuses on top of the damage worked out below.
                aoe.m_owner = instance.m_owner;

                // PvP: the explosion hits what the arrow itself could (vanilla Projectile.IsValidTarget). With
                // the archer's PvP off that is enemies only; with it on, friendlies as well, and vanilla's
                // RPC_Damage then spares any player whose own PvP is off. Every player shares one m_name
                // ("Human"), so m_hitSame has to follow too, or the same-kind filter drops them all.
                bool pvp = instance.m_owner.IsPVPEnabled();
                aoe.m_hitFriendly = pvp;
                aoe.m_hitSame = pvp;
                // Damage the effect deals on its own, not a weapon strike (see HitSource).
                HitSource.MarkBonusSource(spawnedObject);

                float explodingArrowStrength = explodingArrowValue * instance.m_damage.EpicLootGetTotalDamage();
                aoe.m_damage.m_fire = explodingArrowStrength;
            }
        }
    }
}