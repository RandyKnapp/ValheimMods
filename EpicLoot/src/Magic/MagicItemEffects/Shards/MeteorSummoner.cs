using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Summons a meteor on hit after 25 direct weapon hits. Meteor damage is the triggering (final) hit's
    // blockable damage times the shard's multiplier.
    public static class MeteorSummoner {
        private const string MeteorPrefab = "projectile_meteor";
        private const int MaxCharges = 25;           // weapon hits required to charge the meteor
        private const float SpawnHeight = 20f;       // metres above the target the meteor launches from
        private const float MinDistance = 5f;        // min horizontal offset of the launch point from the target
        private const float MaxDistance = 15f;       // max horizontal offset of the launch point from the target
        private const float ProjectileSpeed = 20f;   // metres/second the meteor travels toward the target
        private const float ExplosionRadius = 4f;     // min AOE radius on impact so a moving target is still caught

        private static int _charges;
        private static bool _meteorMissingLogged;

        // Charge HUD indicator (Yagluth trophy icon showing "n/25"). Built lazily on the first charging hit --
        // see GetOrCreateIndicator -- so ObjectDB is loaded when the trophy is queried. Its live count is read
        // by SE_MeteorChargeIndicator through the accessors below.
        private const string IndicatorName = "EL_MeteorSummonerCharge";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private static StatusEffect _indicator;
        private static bool _indicatorMissingLogged;

        public static int CurrentCharges => _charges;
        public static int MaxChargeCount => MaxCharges;

        // Tooltip: "Every {1} weapon hits ... deals {0}x that hit's damage" -- {1} is the charge-count
        // const so the shown number stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.MeteorSummoner,
                value => new object[] { value, (float)MaxCharges });
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

            if (_charges < MaxCharges) {
                ++_charges;
            }

            // A killing blow at full charge holds the charge rather than spending it, so the meteor always
            // gets a live target instead of falling on a corpse.
            if (_charges < MaxCharges || __instance.IsDead()) {
                ShowIndicator(player);
                return;
            }

            _charges = 0;
            SummonMeteor(player, __instance, modifier * damage);
        }

        // Launches the meteor from a random point on a ring MinDistance..MaxDistance around the target, lifted
        // SpawnHeight up, and flies it straight into the target's centre at ProjectileSpeed carrying `fireDamage`.
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

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var horizontalDistance = Random.Range(MinDistance, MaxDistance);
            var spawnPos = targetPos + new Vector3(
                Mathf.Cos(angle) * horizontalDistance,
                SpawnHeight,
                Mathf.Sin(angle) * horizontalDistance);

            var velocity = (targetPos - spawnPos).normalized * ProjectileSpeed;

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
            projectile.m_aoe = Mathf.Max(projectile.m_aoe, ExplosionRadius);
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
