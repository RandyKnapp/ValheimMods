using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Vanilla latches m_beenHitWhileDodging so only the first avoided hit of a roll counts as the perfect
    // dodge (Player.RPC_HitWhileDodging); the latch is cleared when the roll ends (Player.UpdateDodge).
    // A Harmony postfix still runs when the original early-returns on that latch, and the attacker raises
    // the RPC once per collider per hit (Attack's hit loop and hit-list pass, plus Projectile) -- so a
    // single roll through a volley or a wide sweep invokes it many times. Gating on the false->true
    // transition is what keeps the rewards to one per roll, matching vanilla's own stamina/adrenaline.
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_HitWhileDodging))]
    internal static class SharedPerfectDodgeRewardPatch
    {
        [HarmonyPrefix]
        [UsedImplicitly]
        private static void Prefix(Player __instance, out bool __state)
        {
            __state = __instance.m_beenHitWhileDodging;
        }

        [HarmonyPostfix]
        [UsedImplicitly]
        private static void Postfix(Player __instance, bool __state)
        {
            // Already latched (a later hit in the same roll), or vanilla bailed out on !IsOwner().
            if (__state || !__instance.m_beenHitWhileDodging)
            {
                return;
            }

            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            PerfectDodgeGivesHealth.OnPerfectDodge(__instance);
            PerfectDodgeGivesStamina.OnPerfectDodge(__instance);
            PerfectDodgeGivesEitr.OnPerfectDodge(__instance);
            PerfectDodgeGivesSpeed.OnPerfectDodge(__instance);
            PerfectDodge.OnPerfectDodge(__instance);
        }
    }

    public static class PerfectDodgeGivesHealth
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesHealth, 0.01f);
            if (fraction > 0f)
            {
                player.Heal(player.GetMaxHealth() * fraction);
            }
        }
    }

    public static class PerfectDodgeGivesStamina
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesStamina, 0.01f);
            if (fraction > 0f)
            {
                player.AddStamina(player.GetMaxStamina() * fraction);
            }
        }
    }

    public static class PerfectDodgeGivesEitr
    {
        public static void OnPerfectDodge(Player player)
        {
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesEitr, 0.01f);
            if (fraction > 0f && player.GetMaxEitr() > 0f)
            {
                player.AddEitr(player.GetMaxEitr() * fraction);
            }
        }
    }

    // The shard value is the stamina-regen bonus per stack; SE_DodgeMomentum's ModifyStaminaRegen applies it.
    public static class PerfectDodge
    {
        public const int DefaultMaxStacks = 5;
        public const float DefaultBuffDuration = 10f;

        private const string MaxStacksKey = "MaxStacks";
        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxStacksKey, DefaultMaxStacks },
            { BuffDurationKey, DefaultBuffDuration },
        };

        private const string BuffName = "EL_DodgeMomentum";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_DodgeMomentum _buffPrototype;
        private static bool _iconMissingLogged;

        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.PerfectDodge,
                value => new object[] { value, (float)GetMaxStacks() });
        }

        public static void OnPerfectDodge(Player player)
        {
            var regenPerStack = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodge, 0.01f);
            if (regenPerStack <= 0f)
            {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null)
            {
                return;
            }

            var maxStacks = GetMaxStacks();
            var duration = GetBuffDuration();
            var seMan = player.GetSEMan();

            // Restamp the per-stack bonus: the shard set may have changed.
            if (seMan.GetStatusEffect(BuffHash) is SE_DodgeMomentum existing)
            {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.RegenPerStack = regenPerStack;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_DodgeMomentum added) {
                added.Stacks = 1;
                added.RegenPerStack = regenPerStack;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        private static int GetMaxStacks()
        {
            return EffectConfig.GetIntAtLeast(MagicEffectType.PerfectDodge, MaxStacksKey, DefaultMaxStacks, 1);
        }

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the buff permanent.
        private static float GetBuffDuration()
        {
            return Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.PerfectDodge, BuffDurationKey, DefaultBuffDuration));
        }

        // A null icon would render as an invisible HUD entry, so a failed sprite lookup leaves the prototype null.
        private static SE_DodgeMomentum GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/Pink.png");
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    EpicLoot.LogWarning("PerfectDodge: could not load the Pink shardstone sprite; Dodge Momentum will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_DodgeMomentum>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_dodgemomentum";
            se.m_icon = icon;
            se.m_ttl = GetBuffDuration(); // restamped on every proc
            _buffPrototype = se;
            return _buffPrototype;
        }
    }

    // Mirrors ModifyDodgeStaminaUse.
    public static class DecreaseDodgeCost
    {
        [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
        private static class GetEquipmentDodgeStaminaModifier_Patch
        {
            [UsedImplicitly]
            private static void Postfix(Player __instance, ref float __result)
            {
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                __result -= __instance.GetTotalActiveMagicEffectValue(MagicEffectType.DecreaseDodgeCost, 0.01f);
            }
        }
    }

    // A status effect rather than a bare speed patch, so the player gets a HUD icon and tooltip and the speed
    // goes through vanilla's own StatusEffect.ModifySpeed path.
    public static class PerfectDodgeGivesSpeed
    {
        public const float DefaultBuffDuration = 1f;

        private const string BuffDurationKey = "BuffDuration";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { BuffDurationKey, DefaultBuffDuration },
        };

        // Floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would make the burst permanent.
        private static float GetBuffDuration()
        {
            return Mathf.Max(0.1f, EffectConfig.Get(MagicEffectType.PerfectDodgeGivesSpeed,
                BuffDurationKey, DefaultBuffDuration));
        }

        private const string BuffName = "EL_DodgeAgility";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_DodgeAgility _buffPrototype;
        private static bool _iconMissingLogged;

        public static void OnPerfectDodge(Player player)
        {
            var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.PerfectDodgeGivesSpeed, 0.01f);
            if (bonus <= 0f)
            {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null)
            {
                return;
            }

            var seMan = player.GetSEMan();

            // Restamp the bonus: the shard set may have changed.
            var duration = GetBuffDuration();
            if (seMan.GetStatusEffect(BuffHash) is SE_DodgeAgility existing)
            {
                existing.SpeedBonus = bonus;
                existing.m_ttl = duration; // restamped so a retuned duration reaches a buff already running
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_DodgeAgility added) {
                added.SpeedBonus = bonus;
                added.m_ttl = duration;
                added.ResetTime();
            }
        }

        // A null icon would render as an invisible HUD entry, so a failed sprite lookup leaves the prototype null.
        private static SE_DodgeAgility GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            var icon = EpicAssets.AssetBundle?.LoadAsset<Sprite>("Assets/EpicLoot/Sprites/Shardstones/Pink.png");
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    EpicLoot.LogWarning("PerfectDodgeGivesSpeed: could not load the Pink shardstone sprite; Dodge Agility will not display.");
                    _iconMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_DodgeAgility>();
            se.name = BuffName;
            se.m_name = "$mod_epicloot_se_dodgeagility";
            se.m_icon = icon;
            se.m_ttl = GetBuffDuration(); // restamped on every proc
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
