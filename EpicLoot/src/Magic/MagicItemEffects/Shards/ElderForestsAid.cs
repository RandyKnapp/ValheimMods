using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {

    public static class ElderForestsAid {
        // Radius is BaseRadius plus RadiusPerTier per point of value; cooldown is Cooldown MINUS
        // CooldownPerValue per point, floored at MinCooldown.
        public const float DefaultCooldown = 60f;
        public const float DefaultCooldownPerValue = 1.5f;
        public const float DefaultMinCooldown = 10f;
        public const float DefaultBaseRadius = 6f;
        public const float DefaultRadiusPerTier = 0.15f;

        private const string CooldownKey = "Cooldown";
        private const string CooldownPerValueKey = "CooldownPerValue";
        private const string MinCooldownKey = "MinCooldown";
        private const string BaseRadiusKey = "BaseRadius";
        private const string RadiusPerTierKey = "RadiusPerTier";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { CooldownKey, DefaultCooldown },
            { CooldownPerValueKey, DefaultCooldownPerValue },
            { MinCooldownKey, DefaultMinCooldown },
            { BaseRadiusKey, DefaultBaseRadius },
            { RadiusPerTierKey, DefaultRadiusPerTier },
        };

        private static float GetRadius(float value) {
            return EffectConfig.Get(MagicEffectType.ForestsAid, BaseRadiusKey, DefaultBaseRadius)
                + value * EffectConfig.Get(MagicEffectType.ForestsAid, RadiusPerTierKey, DefaultRadiusPerTier);
        }

        // Floored just above zero on top of MinCooldown, because a ttl of 0 is "no timeout" to vanilla, which
        // would gate the shard permanently rather than removing the cooldown.
        private static float GetCooldown(float value) {
            var cooldown = EffectConfig.Get(MagicEffectType.ForestsAid, CooldownKey, DefaultCooldown)
                - value * EffectConfig.Get(MagicEffectType.ForestsAid,
                    CooldownPerValueKey, DefaultCooldownPerValue);
            var floor = EffectConfig.Get(MagicEffectType.ForestsAid, MinCooldownKey, DefaultMinCooldown);
            return Mathf.Max(0.1f, Mathf.Max(floor, cooldown));
        }

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.ForestsAid,
                value => new object[] { GetRadius(value), GetCooldown(value) });
        }

        private const string ImmobilizeSE = "ImmobilizedAshlands";
        private const string HitFxPrefab = "fx_natureweapon_hit";
        private static readonly int ImmobilizeHash = ImmobilizeSE.GetStableHashCode();

        // The cooldown indicator's presence on the player is also the cooldown gate.
        private const string CooldownName = "EL_ForestsAidCooldown";
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

            if (!player.HasActiveMagicEffect(MagicEffectType.ForestsAid, out var value) || value <= 0f) {
                return;
            }

            Immobilize(player, GetRadius(value));
            ShowCooldown(player, GetCooldown(value));
        }

        private static void Immobilize(Player player, float radius) {
            var center = player.transform.position;
            var radiusSqr = radius * radius;
            var fxPrefab = ZNetScene.instance?.GetPrefab(HitFxPrefab);

            foreach (var character in Character.GetAllCharacters()) {
                if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead()
                    || character.IsBoss()) {
                    continue;
                }

                if ((character.transform.position - center).sqrMagnitude > radiusSqr) {
                    continue;
                }

                if (character.m_nview == null || !character.m_nview.IsValid()) {
                    continue;
                }

                // AddStatusEffect(hash) applies the SE on the target's owner (RPCing there if we aren't it),
                // so the root replicates correctly regardless of who owns the enemy.
                character.GetSEMan().AddStatusEffect(ImmobilizeHash, true);

                if (fxPrefab != null) {
                    Object.Instantiate(fxPrefab, character.GetCenterPoint(), Quaternion.identity);
                }
            }
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

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyTheElder")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_cooldownMissingLogged) {
                    EpicLoot.LogWarning("ForestsAid: could not find 'TrophyTheElder' icon; cooldown indicator will not display.");
                    _cooldownMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_forestsaid";
            se.m_icon = icon;
            se.m_ttl = DefaultCooldown;   // overwritten per-proc by ShowCooldown with the value-scaled cooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }
    }
}
