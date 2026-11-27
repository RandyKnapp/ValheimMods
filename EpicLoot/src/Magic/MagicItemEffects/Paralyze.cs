using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public class SE_Paralyzed : StatusEffect
    {
        public void Setup(float lifetime)
        {
            m_ttl = Mathf.Max(lifetime, GetRemaningTime());
            ResetTime();
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            speed *= 0;
        }
    }

    public static class Paralyze
    {
        private const string RpcKey = "epic loot paralyze";

        // NameHash() hashes the object name, so it must be identical on every client.
        private const string SeName = "EL_Paralyze";

        private static SE_Paralyzed _prototype;

        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class AddRpc_Character_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance)
            {
                __instance.m_nview?.Register<float>(RpcKey, (sender, duration) => RPC_Paralyze(__instance, duration));
            }
        }

        // Called from SharedCharacterDamagePatch's postfix. The Paralyze value and Attack_Patch.ActiveAttack are
        // only readable on the attacker's own client; the SE is applied on the target's owner through the RPC.
        public static void OnDamaged(Character __instance, HitData hit, Character attacker)
        {
            // IsValid() (not just a null check): the target can already be dead and destroyed by the time this
            // postfix runs -- ZNetScene.Destroy nulls the ZDO immediately, so InvokeRPC below would NRE.
            if (hit == null || __instance == null || __instance.m_nview == null
                || !__instance.m_nview.IsValid()
                || attacker != Player.m_localPlayer
                || hit.m_damage.EpicLootGetTotalDamage() <= 0.0)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (!player.HasActiveMagicEffect(MagicEffectType.Paralyze, out float effectValue))
            {
                return;
            }

            float totalParalyzeTime = effectValue;
            if (Attack_Patch.ActiveAttack != null)
            {
                totalParalyzeTime = MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
                    player, Attack_Patch.ActiveAttack.m_weapon, MagicEffectType.Paralyze);
            }

            if (totalParalyzeTime <= 0f)
            {
                return;
            }

            __instance.m_nview.InvokeRPC(RpcKey, totalParalyzeTime);
        }

        // Sent to the target's owner only. Vanilla ticks status effects on the owner alone, so a copy added on
        // any other client never expires, and stops the creature again once ownership moves there.
        private static void RPC_Paralyze(Character character, float duration)
        {
            if (character == null || character.m_seman == null || duration <= 0f ||
                character.m_nview == null || !character.m_nview.IsOwner())
            {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (character.m_seman.GetStatusEffect(prototype.NameHash()) is SE_Paralyzed existing)
            {
                existing.Setup(duration);
                return;
            }

            if (character.m_seman.AddStatusEffect(prototype) is SE_Paralyzed added)
            {
                added.Setup(duration);
            }
        }

        private static SE_Paralyzed GetOrCreatePrototype()
        {
            if (_prototype != null)
            {
                return _prototype;
            }

            var se = ScriptableObject.CreateInstance<SE_Paralyzed>();
            se.name = SeName;
            se.m_name = "Paralyzed"; // no icon, so this never renders in the HUD; kept for logs/lookup only
            _prototype = se;
            return _prototype;
        }
    }
}
