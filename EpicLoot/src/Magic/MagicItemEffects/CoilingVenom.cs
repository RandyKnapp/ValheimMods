using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string CoilingVenom = nameof(CoilingVenom);
    }
}

namespace EpicLoot.MagicItemEffects
{
    public static class CoilingVenom
    {
        private const string RpcKey = "EL_CoilVenom";

        public static void ModifyWeaponDamage(ref HitData.DamageTypes damage)
        {
            float fraction = Mathf.Clamp01(Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.CoilingVenom, 0.01f));
            if (fraction <= 0f)
            {
                return;
            }

            float physical = damage.m_blunt + damage.m_slash + damage.m_pierce;
            if (physical <= 0f)
            {
                return;
            }

            damage.m_poison += physical * fraction;
            DamageConversionHelper.RemovePhysicalShare(ref damage, fraction);
        }

        // Sent ahead of the hit's own RPC_Damage and consumed by it on the victim's owner. Routed RPCs from one
        // peer to another arrive in the order they were sent; the stacking depends on that.
        public static void TagOutgoingHit(Character victim, HitData hit, Character attacker)
        {
            if (hit.m_damage.m_poison <= 0f || attacker == null || attacker != Player.m_localPlayer)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (!player.HasActiveMagicEffect(MagicEffectType.CoilingVenom, out float _))
            {
                return;
            }

            if (victim == null || victim.IsPlayer() || victim.IsTamed() || victim.m_nview == null || !victim.m_nview.IsValid())
            {
                return;
            }

            player.HasActiveMagicEffect(MagicEffectType.NineSteps, out float doomThreshold);
            EpicLoot.Log($"[NineSteps] Tagged {Utils.GetPrefabName(victim.gameObject)}: {hit.m_damage.m_poison:0.##} poison outgoing, " +
                (doomThreshold > 0f ? $"doom threshold {doomThreshold:0.#}%" : "Nine Steps not active"));
            victim.m_nview.InvokeRPC(RpcKey, player.GetZDOID(), doomThreshold);
        }

        private static void RPC_CoilVenom(Character character, ZDOID attacker, float doomThreshold)
        {
            if (character == null || character.IsPlayer() || character.m_nview == null ||
                !character.m_nview.IsValid() || !character.m_nview.IsOwner())
            {
                return;
            }

            if (!character.TryGetComponent(out CoiledVenom venom))
            {
                venom = character.gameObject.AddComponent<CoiledVenom>();
            }

            venom.Attacker = attacker;
            venom.DoomThreshold = NineSteps.ClampThreshold(doomThreshold);
            venom.Tagged = true;
        }

        public static void OnIncomingHit(Character victim, HitData hit, Character attacker)
        {
            if (CoiledVenom.Count == 0 || !victim.TryGetComponent(out CoiledVenom venom))
            {
                return;
            }

            venom.Armed = false;
            venom.Added = 0f;
            venom.Carried = 0f;
            if (venom.Tagged && attacker != null && attacker.GetZDOID() == venom.Attacker)
            {
                venom.Tagged = false;
                venom.Armed = hit.m_damage.m_poison > 0f;
            }
        }

        public static void OnDamageTaken(Character victim)
        {
            if (CoiledVenom.Count == 0 || !victim.TryGetComponent(out CoiledVenom venom))
            {
                return;
            }

            bool resisted = venom.Armed;
            venom.Armed = false;
            NineSteps.Evaluate(victim, venom, resisted);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class RegisterRpc_Character_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance)
            {
                __instance.m_nview?.Register<ZDOID, float>(RpcKey,
                    (sender, attacker, doomThreshold) => RPC_CoilVenom(__instance, attacker, doomThreshold));
            }
        }

        // Armed is only set while Character.RPC_Damage works through a tagged hit, so the resistance is lifted
        // for that hit's poison alone.
        [HarmonyPatch(typeof(Character), nameof(Character.GetDamageModifiers))]
        private static class Character_GetDamageModifiers_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance, ref HitData.DamageModifiers __result)
            {
                if (CoiledVenom.Count == 0 || !__instance.TryGetComponent(out CoiledVenom venom) || !venom.Armed)
                {
                    return;
                }

                switch (__result.m_poison)
                {
                    case HitData.DamageModifier.SlightlyResistant:
                    case HitData.DamageModifier.Resistant:
                    case HitData.DamageModifier.VeryResistant:
                        __result.m_poison = HitData.DamageModifier.Normal;
                        break;
                }
            }
        }

        // The carried venom is added here rather than to the outgoing hit: this is past the victim's resistances
        // and the backstab and stagger multipliers, which would otherwise be applied to the old venom again on
        // every hit.
        [HarmonyPatch(typeof(Character), nameof(Character.AddPoisonDamage))]
        private static class Character_AddPoisonDamage_Patch
        {
            [UsedImplicitly]
            private static bool Prefix(Character __instance, ref float damage)
            {
                if (CoiledVenom.Count == 0 || damage <= 0f || !__instance.TryGetComponent(out CoiledVenom venom))
                {
                    return true;
                }

                if (venom.Doomed)
                {
                    return false;
                }

                if (!venom.Armed)
                {
                    return true;
                }

                venom.Armed = false;
                venom.Added = damage;
                if (__instance.GetSEMan().GetStatusEffect(SEMan.s_statusEffectPoison) is SE_Poison poison)
                {
                    venom.Carried = Mathf.Max(0f, poison.m_damageLeft);
                    damage += venom.Carried;
                }

                return true;
            }
        }
    }

    internal sealed class CoiledVenom : MonoBehaviour
    {
        internal static int Count;

        internal ZDOID Attacker;
        internal float DoomThreshold;
        internal bool Tagged;
        internal bool Armed;
        internal float Added;
        internal float Carried;
        internal NineStepsDoom Doom;

        internal bool Doomed => Doom != null;

        [UsedImplicitly]
        private void Awake()
        {
            Count++;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            Count--;
        }
    }
}
