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
        // Routed RPC key used to apply the paralyze on the target's OWNER, where movement/AI is
        // authoritative. Mirrors Slow.RPCKey.
        private const string RpcKey = "epic loot paralyze";

        // Unity object name of the SE prototype -- NameHash() hashes this (GetStableHashCode), so it must be
        // identical on every client for the add/refresh lookup to line up.
        private const string SeName = "EL_Paralyze";

        private static SE_Paralyzed _prototype;

        // Registers the paralyze RPC on every character so a remote-owned target can receive it. Mirrors
        // SlowAddRPC_Character_Awake_Patch.
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class AddRpc_Character_Awake_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Character __instance)
            {
                __instance.m_nview?.Register<float>(RpcKey, (sender, duration) => RPC_Paralyze(__instance, duration));
            }
        }

        // Attacker-side check invoked by CharacterDamageDispatch (Character.Damage postfix). The local player's
        // Paralyze value and Attack_Patch.ActiveAttack are only readable on the attacker's own client, so the
        // check must happen here; the SE itself is applied on the target's owner via the RPC below.
        public static void OnDamaged(Character __instance, HitData hit, Character attacker)
        {
            if (hit == null || __instance == null || __instance.m_nview == null
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

            // Broadcast so the target's owner (and everyone else, harmlessly) adds/refreshes the effect.
            __instance.m_nview.InvokeRPC(ZRoutedRpc.Everybody, RpcKey, totalParalyzeTime);
        }

        // Adds or refreshes SE_Paralyzed on the character. Runs on every client; only the owner's copy drives
        // movement/AI, so this is what actually stops a remote-owned target.
        private static void RPC_Paralyze(Character character, float duration)
        {
            if (character == null || character.m_seman == null || duration <= 0f)
            {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (character.m_seman.GetStatusEffect(prototype.NameHash()) is SE_Paralyzed existing)
            {
                existing.Setup(duration);
                return;
            }

            // AddStatusEffect(prototype) clones via MemberwiseClone (keeps NameHash), then we set the duration.
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
