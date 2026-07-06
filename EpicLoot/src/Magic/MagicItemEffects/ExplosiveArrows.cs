using EpicLoot.General;
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

                float explodingArrowStrength = explodingArrowValue * instance.m_damage.EpicLootGetTotalDamage();
                aoe.m_damage.m_fire = explodingArrowStrength;
            }
        }
    }
}