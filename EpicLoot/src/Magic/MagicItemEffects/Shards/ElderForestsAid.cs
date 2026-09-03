using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {

    // When the player takes damage, immobilizes nearby enemies for a short duration. The radius and cooldown scale with the shard value.
    public static class ElderForestsAid {
        // Both the radius and the cooldown scale with the shard value: radius is BaseRadius plus
        // RadiusPerTier per point, cooldown is Cooldown plus CooldownPerValue per point -- so a stronger
        // shard reaches further but fires less often. All tunable in this effect's Config block in
        // config/shardstones.json, under these key names.
        public const float DefaultCooldown = 30f;
        public const float DefaultCooldownPerValue = 1.5f;
        public const float DefaultBaseRadius = 6f;
        public const float DefaultRadiusPerTier = 0.15f;

        private const string CooldownKey = "Cooldown";
        private const string CooldownPerValueKey = "CooldownPerValue";
        private const string BaseRadiusKey = "BaseRadius";
        private const string RadiusPerTierKey = "RadiusPerTier";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { CooldownKey, DefaultCooldown },
            { CooldownPerValueKey, DefaultCooldownPerValue },
            { BaseRadiusKey, DefaultBaseRadius },
            { RadiusPerTierKey, DefaultRadiusPerTier },
        };

        private const string ImmobilizeSE = "ImmobilizedAshlands";
        private const string HitFxPrefab = "fx_natureweapon_hit";
        private static readonly int ImmobilizeHash = ImmobilizeSE.GetStableHashCode();

        // Cooldown HUD indicator (The Elder trophy icon with a radial recharge sweep). Built lazily on the
        // first proc -- see GetOrCreateCooldownIndicator -- so ObjectDB is loaded when the trophy is queried.
        // Its presence on the player is also the cooldown gate (checked via CooldownHash below).
        private const string CooldownName = "EL_ForestsAidCooldown";
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

            if (!player.HasActiveMagicEffect(MagicEffectType.ForestsAid, out var value) || value <= 0f) {
                return;
            }

            var radius = EffectConfig.Get(MagicEffectType.ForestsAid, BaseRadiusKey, DefaultBaseRadius)
                + value * EffectConfig.Get(MagicEffectType.ForestsAid, RadiusPerTierKey, DefaultRadiusPerTier);
            // Cooldown floored just above zero: a ttl of 0 is "no timeout" to vanilla, which would gate the
            // shard permanently rather than removing the cooldown.
            var cooldown = Mathf.Max(0.1f,
                EffectConfig.Get(MagicEffectType.ForestsAid, CooldownKey, DefaultCooldown)
                + value * EffectConfig.Get(MagicEffectType.ForestsAid,
                    CooldownPerValueKey, DefaultCooldownPerValue));

            Immobilize(player, radius);
            ShowCooldown(player, cooldown);
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

        // Adds the recharge indicator to the player with the value-scaled cooldown as its lifetime. We set
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

        // Lazily builds the cooldown indicator prototype. Runs on a proc, so ObjectDB is loaded and the Elder
        // trophy icon is available. A null icon would render as an invisible HUD entry, so if the trophy
        // lookup fails we log once and leave _cooldownIndicator null.
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
