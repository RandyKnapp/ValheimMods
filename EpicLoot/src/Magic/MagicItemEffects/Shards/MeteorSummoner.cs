using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Summons a meteor on hit after 25 direct weapon hits. Meteor damage is the triggering (final) hit's
    // blockable damage times the shard's multiplier.
    public static class MeteorSummoner {
        private const string MeteorPrefab = "projectile_meteor";

        // All tunable in this effect's Config block in config/shardstones.json, under these key names.
        public const int DefaultMaxCharges = 25;             // weapon hits required to charge the meteor
        public const float DefaultSpawnHeight = 20f;         // metres above the target the meteor launches from
        public const float DefaultMinDistance = 5f;          // min horizontal offset of the launch point
        public const float DefaultMaxDistance = 15f;         // max horizontal offset of the launch point
        public const float DefaultProjectileSpeed = 20f;     // metres/second the meteor travels
        public const float DefaultExplosionRadius = 4f;      // min AOE radius so a moving target is still caught

        private const string MaxChargesKey = "MaxCharges";
        private const string SpawnHeightKey = "SpawnHeight";
        private const string MinDistanceKey = "MinDistance";
        private const string MaxDistanceKey = "MaxDistance";
        private const string ProjectileSpeedKey = "ProjectileSpeed";
        private const string ExplosionRadiusKey = "ExplosionRadius";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { MaxChargesKey, DefaultMaxCharges },
            { SpawnHeightKey, DefaultSpawnHeight },
            { MinDistanceKey, DefaultMinDistance },
            { MaxDistanceKey, DefaultMaxDistance },
            { ProjectileSpeedKey, DefaultProjectileSpeed },
            { ExplosionRadiusKey, DefaultExplosionRadius },
        };

        // Clamped to at least 1 so a misconfiguration can't make the meteor unreachable or fire every hit
        // through a zero/negative threshold.
        private static int GetMaxCharges() {
            return EffectConfig.GetIntAtLeast(MagicEffectType.MeteorSummoner,
                MaxChargesKey, DefaultMaxCharges, 1);
        }

        private static int _charges;
        private static bool _meteorMissingLogged;

        // Charge HUD indicator (Yagluth trophy icon showing "n/max"). Built lazily on the first charging hit --
        // see GetOrCreateIndicator -- so ObjectDB is loaded when the trophy is queried. Its live count is read
        // by SE_MeteorChargeIndicator through the accessors below.
        private const string IndicatorName = "EL_MeteorSummonerCharge";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private static StatusEffect _indicator;
        private static bool _indicatorMissingLogged;

        public static int CurrentCharges => _charges;
        public static int MaxChargeCount => GetMaxCharges();

        // Tooltip: "Every {1} weapon hits ... deals {0}x that hit's damage" -- {1} is the configured
        // charge count, so the shown number follows a retune instead of the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.MeteorSummoner,
                value => new object[] { value, (float)GetMaxCharges() });
        }

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player || attacker != player) {
                return;
            }

            float modifier = player.GetTotalActiveMagicEffectValue(MagicEffectType.MeteorSummoner, 1f);
            if (modifier <= 0f) {
                // indicator self-removes via SE_MeteorChargeIndicator.IsDone
                _charges = 0;
                return;
            }

            // Only direct weapon attacks charge the meteor: proc damage -- this shard's own meteor included --
            // carries no skill. Chop/pickaxe damage sits outside GetTotalBlockableDamage, so mining and
            // chopping never charge either, and that same blockable total is what the meteor multiplies.
            float damage = hit.GetTotalBlockableDamage();
            if (hit.m_skill == Skills.SkillType.None || damage <= 0f) {
                return;
            }

            if (_charges < GetMaxCharges()) {
                ++_charges;
            }

            // A killing blow at full charge holds the charge rather than spending it, so the meteor always
            // gets a live target instead of falling on a corpse.
            if (_charges < GetMaxCharges() || __instance.IsDead()) {
                ShowIndicator(player);
                return;
            }

            _charges = 0;
            SummonMeteor(player, __instance, modifier * damage);
        }

        // Launches the meteor from a random point on a ring MinDistance..MaxDistance around the target, lifted
        // SpawnHeight up, and flies it straight into the target's centre at ProjectileSpeed carrying `fireDamage`.
        // All four are read from the effect Config; see the key constants above.
        private static void SummonMeteor(Player player, Character target, float fireDamage) {
            var prefab = ZNetScene.instance?.GetPrefab(MeteorPrefab);
            if (prefab == null) {
                if (!_meteorMissingLogged) {
                    EpicLoot.LogWarning($"MeteorSummoner: could not find '{MeteorPrefab}' prefab; meteor will not spawn.");
                    _meteorMissingLogged = true;
                }
                return;
            }

            var targetPos = target.GetCenterPoint();

            var minDistance = EffectConfig.Get(MagicEffectType.MeteorSummoner,
                MinDistanceKey, DefaultMinDistance);
            // Held at or above the min so Random.Range can't be handed an inverted span.
            var maxDistance = Mathf.Max(minDistance, EffectConfig.Get(MagicEffectType.MeteorSummoner,
                MaxDistanceKey, DefaultMaxDistance));

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var horizontalDistance = Random.Range(minDistance, maxDistance);
            var spawnPos = targetPos + new Vector3(
                Mathf.Cos(angle) * horizontalDistance,
                EffectConfig.Get(MagicEffectType.MeteorSummoner, SpawnHeightKey, DefaultSpawnHeight),
                Mathf.Sin(angle) * horizontalDistance);

            // Floored above zero: a speed of 0 would leave the meteor hanging in the air forever.
            var speed = Mathf.Max(0.1f, EffectConfig.Get(MagicEffectType.MeteorSummoner,
                ProjectileSpeedKey, DefaultProjectileSpeed));
            var velocity = (targetPos - spawnPos).normalized * speed;

            var meteor = Object.Instantiate(prefab, spawnPos, Quaternion.LookRotation(velocity));
            var projectile = meteor.GetComponent<Projectile>();
            if (projectile == null) {
                return;
            }

            // Straight-line shot: zero gravity so the fixed speed carries the meteor into the target instead
            // of arcing short, and disable the owner raytest (meant for player weapons fired from the chest --
            // it would false-hit from the player's position the instant this spawns 20 m away).
            projectile.m_gravity = 0f;
            projectile.m_doOwnerRaytest = false;

            // The hit is deliberately left with m_skill = None: Projectile copies m_skill onto every hit it
            // lands, and the skill check in OnDamageDealt is what stops those hits from building charges.
            var hitData = new HitData { m_damage = { m_fire = fireDamage } };
            hitData.SetAttacker(player);
            projectile.Setup(player, velocity, -1f, hitData, null, null);

            // projectile_meteor is Yagluth's meteor: it carries m_onlySpawnedProjectilesDealDamage with a
            // ground-only AOE spawn, so Setup ZEROES m_damage above and defers all damage to a sub-object that
            // only spawns on a terrain hit. Striking the character directly therefore lands nothing. Re-apply
            // our fire hit after Setup and make the meteor itself deal it, so hitting the target does damage
            // regardless of the prefab's spawn-on-hit / ground-only rules. A guaranteed AOE radius ensures a
            // moving target still gets caught when the meteor lands beside it instead of dead-on.
            projectile.m_damage = hitData.m_damage;
            projectile.m_onlySpawnedProjectilesDealDamage = false;
            projectile.m_aoe = Mathf.Max(projectile.m_aoe, EffectConfig.Get(
                MagicEffectType.MeteorSummoner, ExplosionRadiusKey, DefaultExplosionRadius));
        }

        // Adds the charge HUD indicator to the player if it isn't already showing. AddStatusEffect clones the
        // prototype and no-ops when an effect with the same NameHash is present, so the HaveStatusEffect guard
        // just skips building the prototype on repeat hits.
        private static void ShowIndicator(Player player) {
            var seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(IndicatorHash)) {
                return;
            }

            var indicator = GetOrCreateIndicator();
            if (indicator != null) {
                seMan.AddStatusEffect(indicator);
            }
        }

        // Lazily builds the indicator prototype. Runs on a hit, so ObjectDB is loaded and the Yagluth (goblin
        // king) trophy icon is available. A null icon would render as an invisible HUD entry (SEMan only
        // surfaces effects with an icon), so if the trophy lookup fails we log once and leave _indicator null.
        private static StatusEffect GetOrCreateIndicator() {
            if (_indicator != null) {
                return _indicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab("TrophyGoblinKing")?
                .GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null) {
                if (!_indicatorMissingLogged) {
                    EpicLoot.LogWarning("MeteorSummoner: could not find 'TrophyGoblinKing' icon; charge indicator will not display.");
                    _indicatorMissingLogged = true;
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<SE_MeteorChargeIndicator>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_se_meteorsummoner";
            se.m_icon = icon;
            se.m_ttl = 0f;
            _indicator = se;
            return _indicator;
        }
    }
}
