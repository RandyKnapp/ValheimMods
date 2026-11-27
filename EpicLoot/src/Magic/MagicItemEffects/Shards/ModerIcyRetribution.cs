using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ModerIcyRetribution {
        public const float DefaultRadius = 8f;
        public const float DefaultFrostPerTier = 8f;       // frost damage per point of value
        public const float DefaultBaseCooldown = 200f;     // cooldown at Epic (the shard's rarity floor)
        public const float DefaultCooldownPerRarity = 20f; // removed per rarity above Epic
        public const float DefaultMinCooldown = 30f;       // floor, so a retune cannot make it near-instant

        private const string RadiusKey = "Radius";
        private const string FrostPerTierKey = "FrostPerTier";
        private const string BaseCooldownKey = "BaseCooldown";
        private const string CooldownPerRarityKey = "CooldownPerRarity";
        private const string MinCooldownKey = "MinCooldown";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { RadiusKey, DefaultRadius },
            { FrostPerTierKey, DefaultFrostPerTier },
            { BaseCooldownKey, DefaultBaseCooldown },
            { CooldownPerRarityKey, DefaultCooldownPerRarity },
            { MinCooldownKey, DefaultMinCooldown },
        };

        // The cooldown is keyed off the shard's rarity rather than its value, and a provider only receives
        // the value, so the cooldown stays in the Shift-detail config lines instead.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.IcyRetribution,
                value => new object[] {
                    value * EffectConfig.Get(MagicEffectType.IcyRetribution,
                        FrostPerTierKey, DefaultFrostPerTier),
                });
        }

        private const string NovaTemplateName = "EL_ModerIcyRetributionNova";

        public static void RegisterNovaPrefab() {
            FrostNovaFx.RegisterPrefab(NovaTemplateName);
        }

        // The cooldown indicator's presence on the player is also the cooldown gate.
        private const string CooldownName = "EL_IcyRetributionCooldown";
        private static readonly int CooldownHash = CooldownName.GetStableHashCode();
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;

        // Postfix handler invoked by SharedCharacterRpcDamagePatch (on-damage-taken reaction).
        public static void OnDamageTaken(Character __instance, HitData hit) {
            var player = Player.m_localPlayer;
            if (__instance != player || hit == null) {
                return;
            }

            if (player.GetSEMan().HaveStatusEffect(CooldownHash)) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.IcyRetribution, 1f);
            if (value <= 0f) {
                return;
            }
            // Just above the player's feet, so the nova is visible but close to the ground.
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

        // Shortens with the highest rarity among the equipped IcyRetribution shards, never past MinCooldown.
        // Floored just above zero on top of that: a ttl of 0 is "no timeout" to vanilla, which would gate the
        // shard permanently.
        private static float GetCooldown(Player player) {
            var stepsAboveEpic = Mathf.Max(0, (int)GetEffectRarity(player) - (int)ItemRarity.Epic);
            var baseCooldown = EffectConfig.Get(MagicEffectType.IcyRetribution,
                BaseCooldownKey, DefaultBaseCooldown);
            var perRarity = EffectConfig.Get(MagicEffectType.IcyRetribution,
                CooldownPerRarityKey, DefaultCooldownPerRarity);
            var floor = EffectConfig.Get(MagicEffectType.IcyRetribution, MinCooldownKey, DefaultMinCooldown);
            return Mathf.Max(0.1f, Mathf.Max(floor, baseCooldown - stepsAboveEpic * perRarity));
        }

        // Defaults to Epic, the shard's rarity floor.
        private static ItemRarity GetEffectRarity(Player player) {
            var rarity = ItemRarity.Epic;
            var found = false;
            foreach (var item in player.GetMagicEquipment()) {
                if (!item.IsMagic(out var magicItem)) {
                    continue;
                }
                foreach (var socket in magicItem.Sockets) {
                    if (socket == null || !socket.IsActive() || socket.Effect.EffectType != MagicEffectType.IcyRetribution) {
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

        // m_ttl is set on the shared prototype before adding; AddStatusEffect clones it (MemberwiseClone), so
        // the added instance carries this cooldown.
        private static void ShowCooldown(Player player, float cooldown) {
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null) {
                indicator.m_ttl = cooldown;
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // Built on the first proc, when ObjectDB is loaded. A null icon would render as an invisible HUD entry,
        // so a failed trophy lookup leaves the indicator null.
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
