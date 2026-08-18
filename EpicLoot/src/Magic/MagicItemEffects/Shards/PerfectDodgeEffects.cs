using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Perfect dodge specific rewards

    // ---- Shared trigger for the reward effects ------------------------------------------------------
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

    // ---- Rewards on a perfect dodge: restore a % of the matching max pool -------------------------
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

    // ---- Trinket: a perfect dodge builds a stacking stamina-regen buff -----------------------------
    // Grants "Dodge Momentum" (SE_DodgeMomentum) for BuffDuration seconds. Each perfect dodge adds a stack up
    // to MaxStacks and refreshes the countdown, so chaining dodges through a fight ramps the regen and holds
    // it -- paying back the stamina the dodging itself costs. The shard value is the bonus *per stack*, so a
    // Mythic (12) tops out at +60% stamina regen. The regen is applied by SE_DodgeMomentum's own
    // ModifyStaminaRegen override, so vanilla drives it every frame off the live stack count.
    public static class PerfectDodge
    {
        public const int DefaultMaxStacks = 5;
        private const float BuffDuration = 10f; // seconds the buff lasts / is refreshed to on each perfect dodge

        private const string BuffName = "EL_DodgeMomentum";
        private static readonly int BuffHash = BuffName.GetStableHashCode();
        private static SE_DodgeMomentum _buffPrototype;
        private static bool _iconMissingLogged;

        // Tooltip: "+{0}% Stamina Regen per Stack (max {1})" -- {1} surfaces the configured stack cap. Pure,
        // as the provider contract requires (MagicItem.RegisterDisplayValues): it only reads the effect config.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.PerfectDodge,
                value => new object[] { value, (float)GetMaxStacks() });
        }

        public static void OnPerfectDodge(Player player)
        {
            // Shard value doubles as the per-stack stamina-regen percentage (8 -> +8% per stack at Epic).
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
            var seMan = player.GetSEMan();

            // Re-proc while the buff is still up: add a stack (capped), restamp the per-stack bonus (the shard
            // set may have changed) and refresh the countdown rather than letting the old timer run out.
            if (seMan.GetStatusEffect(BuffHash) is SE_DodgeMomentum existing)
            {
                existing.Stacks = Mathf.Min(existing.Stacks + 1, maxStacks);
                existing.MaxStacks = maxStacks;
                existing.RegenPerStack = regenPerStack;
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(GetOrCreatePrototype()) is SE_DodgeMomentum added) {
                added.Stacks = 1;
                added.MaxStacks = maxStacks;
                added.RegenPerStack = regenPerStack;
                added.m_ttl = BuffDuration;
                added.ResetTime();
            }
        }

        // Max stacks come from the PerfectDodge magic effect's Config block ("MaxStacks", see
        // ShardEffectDefinitions), defaulting to DefaultMaxStacks when unset. Clamped to at least 1 so a
        // misconfiguration can't disable the buff.
        private static int GetMaxStacks()
        {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.PerfectDodge);
            if (cfg != null && cfg.TryGetValue("MaxStacks", out var raw))
            {
                return Mathf.Max(1, Mathf.RoundToInt(raw));
            }
            return DefaultMaxStacks;
        }

        // Lazily builds the buff prototype. Runs on a perfect dodge, so the asset bundle is loaded. A null
        // icon would render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if the
        // sprite lookup fails we log once and leave the prototype null.
        private static SE_DodgeMomentum GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            // The Pink (Dodge) shardstone's own icon -- same sprite the shard items use (see Shards.cs).
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
            se.m_ttl = BuffDuration;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }

    // ---- Head: reduce dodge-roll stamina cost (mirrors ModifyDodgeStaminaUse) ----------------------
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

    // ---- Shoulder: a perfect dodge grants a brief burst of movement speed ---------------------------
    // Hangs off the same vanilla perfect-dodge trigger as the reward effects above, granting the "Dodge
    // Agility" buff (SE_DodgeAgility) for BuffDuration seconds. Going through a real status effect rather
    // than a bare speed patch means the player gets a HUD icon and tooltip, and the speed itself is applied
    // through vanilla's own StatusEffect.ModifySpeed path. Shard values are authored as whole-number
    // percents, hence the 0.01f.
    public static class PerfectDodgeGivesSpeed
    {
        private const float BuffDuration = 1f; // seconds the speed buff lasts after a perfect dodge

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

            // Re-proc while the buff is still up: restamp the bonus (the shard set may have changed) and
            // refresh the countdown rather than letting the old, shorter timer run out.
            if (seMan.GetStatusEffect(BuffHash) is SE_DodgeAgility existing)
            {
                existing.SpeedBonus = bonus;
                existing.ResetTime();
                return;
            }

            if (seMan.AddStatusEffect(prototype) is SE_DodgeAgility added) {
                added.SpeedBonus = bonus;
                added.ResetTime();
            }
        }

        // Lazily builds the buff prototype. Runs on a perfect dodge, so the asset bundle is loaded. A null
        // icon would render as an invisible HUD entry (SEMan only surfaces effects with an icon), so if the
        // sprite lookup fails we log once and leave the prototype null.
        private static SE_DodgeAgility GetOrCreatePrototype()
        {
            if (_buffPrototype != null)
            {
                return _buffPrototype;
            }

            // The Pink (Dodge) shardstone's own icon -- same sprite the shard items use (see Shards.cs).
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
            se.m_ttl = BuffDuration;
            _buffPrototype = se;
            return _buffPrototype;
        }
    }
}
