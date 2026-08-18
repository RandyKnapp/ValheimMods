using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Summons a number of tamed bats around the player when they activate adrenaline
    public static class SummonBatWhenActivatingAdrenaline {
        private const string BatSourcePrefab = "Bat";              // vanilla creature we clone
        private const string TamedBatPrefab = "EL_TamedBat";       // our registered player-faction/tamed clone
        private const float BatLifetime = 30f;                     // seconds a summoned bat lives (TimedDestruction)
        private const float SummonCooldown = 10f;
        private const float SpawnRadius = 2f;                      // ring radius bats spawn on around the player

        private static float _lastSummonTime = -999f;

        // Cached template and container for our tamed-bat clone. Must be disabled to prevent znet issues when building the clone
        private static GameObject _batContainer;
        private static GameObject _batTemplate;
        private static bool _sourceMissingLogged;
        private static bool _prefabMissingLogged;

        // Summoned bats this session, tracked so we can enforce the max-concurrent cap. Reloaded bats are not
        // in this list, but TimedDestruction reaps them, so concurrency stays bounded regardless.
        private static readonly List<GameObject> _activeBats = new List<GameObject>();

        // Called by SharedPlayerAddAdrenalinePatch, which owns the Player.AddAdrenaline patch and the
        // fill/pop detection (including the local-player and no-adrenaline-source guards).
        public static void OnAdrenalineActivated(Player player) {
            if (Time.time - _lastSummonTime < SummonCooldown) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.SummonBatWhenActivatingAdrenaline);
            if (value <= 0f) {
                return;
            }

            _lastSummonTime = Time.time;
            SummonBats(player, value);
        }

        public static void RegisterTamedBatPrefab() {
            var zns = ZNetScene.instance;
            if (zns == null || zns.GetPrefab(TamedBatPrefab) != null) {
                return;
            }

            var template = GetOrBuildTemplate(zns);
            if (template == null) {
                return;
            }

            if (!zns.m_prefabs.Contains(template)) {
                zns.m_prefabs.Add(template);
            }
            zns.m_namedPrefabs[TamedBatPrefab.GetStableHashCode()] = template;
            EpicLoot.Log($"SummonBatWhenActivatingAdrenaline: registered '{TamedBatPrefab}' with ZNetScene.");
        }

        private static GameObject GetOrBuildTemplate(ZNetScene zns) {
            if (_batTemplate != null) {
                return _batTemplate;
            }

            var source = zns.GetPrefab(BatSourcePrefab);
            if (source == null) {
                if (!_sourceMissingLogged) {
                    EpicLoot.LogWarning($"SummonBatWhenActivatingAdrenaline: could not find '{BatSourcePrefab}' prefab; bats will not summon.");
                    _sourceMissingLogged = true;
                }
                return null;
            }

            if (_batContainer == null) {
                _batContainer = new GameObject("EL_TamedBatContainer");
                _batContainer.SetActive(false);
                Object.DontDestroyOnLoad(_batContainer);
            }

            var template = Object.Instantiate(source, _batContainer.transform);
            template.name = TamedBatPrefab;

            var character = template.GetComponent<Character>();
            if (character != null) {
                character.m_faction = Character.Faction.Players;
                character.m_tamed = true;
            }

            var timed = template.GetComponent<TimedDestruction>() ?? template.AddComponent<TimedDestruction>();
            timed.m_timeout = BatLifetime;
            timed.m_triggerOnAwake = true;

            _batTemplate = template;
            return _batTemplate;
        }

        // Spawns rarity-scaled bats around the player and enforces the max-concurrent cap. Number summoned rises
        // by one each rarity (effect value 2..6 for Magic..Mythic -> 1..5), and at most 2x that may be alive.
        private static void SummonBats(Player player, float value) {
            if (ZNetScene.instance == null) {
                return;
            }

            var prefab = ZNetScene.instance.GetPrefab(TamedBatPrefab);
            if (prefab == null) {
                if (!_prefabMissingLogged) {
                    EpicLoot.LogWarning($"SummonBatWhenActivatingAdrenaline: '{TamedBatPrefab}' prefab not registered; bats will not summon.");
                    _prefabMissingLogged = true;
                }
                return;
            }

            var summonCount = Mathf.Max(1, Mathf.RoundToInt(value) - 1);
            var maxBats = summonCount * 2;

            // Drop bats that have already despawned/been destroyed before counting toward the cap.
            _activeBats.RemoveAll(bat => bat == null);

            var basePos = player.transform.position + Vector3.up;
            for (var i = 0; i < summonCount; i++) {
                var angle = Mathf.PI * 2f * i / summonCount;
                var spawnPos = basePos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * SpawnRadius;

                var bat = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
                // Faction/tamed/lifetime are baked into the prefab; only the runtime follow target is per-instance.
                bat.GetComponent<MonsterAI>()?.SetFollowTarget(player.gameObject);
                _activeBats.Add(bat);
            }

            // Enforce the cap by despawning the oldest bats first.
            while (_activeBats.Count > maxBats) {
                var oldest = _activeBats[0];
                _activeBats.RemoveAt(0);
                if (oldest != null) {
                    ZNetScene.instance.Destroy(oldest);
                }
            }
        }
    }
}
