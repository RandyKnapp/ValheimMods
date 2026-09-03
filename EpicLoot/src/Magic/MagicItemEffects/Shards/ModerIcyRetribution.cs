using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a reactive frost nova when the player takes damage, with a scaling damage and cooldown based on rarity.
    public static class ModerIcyRetribution {
        // All tunable in this effect's Config block in config/shardstones.json, under these key names.
        public const float DefaultRadius = 8f;
        public const float DefaultFrostPerTier = 8f;       // frost damage per point of value (15..25 -> 120..200)
        public const float DefaultBaseCooldown = 140f;     // cooldown at Epic (the shard's rarity floor)
        public const float DefaultCooldownPerRarity = 20f; // added per rarity above Epic

        private const string RadiusKey = "Radius";
        private const string FrostPerTierKey = "FrostPerTier";
        private const string BaseCooldownKey = "BaseCooldown";
        private const string CooldownPerRarityKey = "CooldownPerRarity";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { RadiusKey, DefaultRadius },
            { FrostPerTierKey, DefaultFrostPerTier },
            { BaseCooldownKey, DefaultBaseCooldown },
            { CooldownPerRarityKey, DefaultCooldownPerRarity },
        };

        // Visual: our own trimmed copy of the fenring's ice nova, built and cached by FrostNovaFx so the
        // fenring's full-length nova is left untouched. Played at the helper's default speed.
        private const string NovaTemplateName = "EL_ModerIcyRetributionNova";

        // Cooldown HUD indicator (Moder trophy icon with a radial recharge sweep). Built lazily on the first
        // proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried. Its
        // presence on the player is also the cooldown gate (checked via CooldownHash below).
        private const string CooldownName = "EL_IcyRetributionCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Postfix handler invoked by CharacterRpcDamageDispatch (on-damage-taken reaction).
        public static void OnDamageTaken(Character __instance, HitData hit) {
            var player = Player.m_localPlayer;
            if (__instance != player || hit == null) {
                return;
            }

            // The visible cooldown status effect is the gate: while it's present the shard stays inert.
            if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.IcyRetribution, 1f);
            if (value <= 0f) {
                return;
            }
            // Spawn the nova at visibly above the players feet, but close to the ground
            Vector3 playerNovaPosition = player.transform.position;
            playerNovaPosition.y += 0.6f;
            FrostNovaFx.Spawn(NovaTemplateName, playerNovaPosition);
            DamageInRadius.DamageEnemiesInRadius(player, player.GetCenterPoint(),
                EffectConfig.Get(MagicEffectType.IcyRetribution, RadiusKey, DefaultRadius),
                new HitData.DamageTypes {
                    m_frost = value * EffectConfig.Get(MagicEffectType.IcyRetribution,
                        FrostPerTierKey, DefaultFrostPerTier)
                });
            ShowCooldown(player, GetCooldown(player));
        }

        // Cooldown length scales with the highest rarity among the equipped IcyRetribution shards: by default
        // 140s at Epic, +20s for each rarity above it (Legendary 160s, Mythic 180s). Floored just above zero:
        // a ttl of 0 is "no timeout" to vanilla, which would gate the shard permanently.
        private static float GetCooldown(Player player) {
            var stepsAboveEpic = Mathf.Max(0, (int)GetEffectRarity(player) - (int)ItemRarity.Epic);
            var baseCooldown = EffectConfig.Get(MagicEffectType.IcyRetribution,
                BaseCooldownKey, DefaultBaseCooldown);
            var perRarity = EffectConfig.Get(MagicEffectType.IcyRetribution,
                CooldownPerRarityKey, DefaultCooldownPerRarity);
            return Mathf.Max(0.1f, baseCooldown + stepsAboveEpic * perRarity);
        }

        // Highest source rarity among the socketed IcyRetribution effects on the player's equipped magic
        // items. Defaults to Epic (the shard's rarity floor) if none is found.
        private static ItemRarity GetEffectRarity(Player player) {
            var rarity = ItemRarity.Epic;
            var found = false;
            foreach (var item in player.GetMagicEquipment()) {
                if (!item.IsMagic(out var magicItem)) {
                    continue;
                }
                foreach (var socket in magicItem.Sockets) {
                    if (socket?.Effect == null || socket.Effect.EffectType != MagicEffectType.IcyRetribution) {
                        continue;
                    }
                    if (!found || socket.SourceRarity > rarity) {
                        rarity = socket.SourceRarity;
                        found = true;
                    }
                }
            }
            return rarity;
        }

        // Adds the recharge indicator to the player with the rarity-scaled cooldown as its lifetime. We set
        // m_ttl on the shared prototype before adding; AddStatusEffect clones it (MemberwiseClone), so the
        // added instance carries this cooldown. Activation is gated on the effect's absence, so it's never
        // already present here.
        private static void ShowCooldown(Player player, float cooldown) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                indicator.m_ttl = cooldown;
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the Moder
        // (dragon) trophy icon is available. A null icon would render as an invisible HUD entry, so if the
        // trophy lookup fails we log once and leave _cooldownIndicator null.
        private static StatusEffect GetOrCreateCooldownIndicator() {
            if (_cooldownIndicator != null) {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyDragonQueen")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("ModerIcyRetribution: could not find 'TrophyDragonQueen' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_icyretribution";
            se.m_icon = icon;
            se.m_ttl = DefaultBaseCooldown;   // overwritten per-proc by ShowCooldown with the rarity-scaled cooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
