using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class FrostAOE
    {
        [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
        public class Attack_DoMeleeAttack_Transpiler
        {
            public const string FxPrefabName = "IceSpikes";
            public static bool Modified;

            public static bool Prefix(Attack __instance)
            {
                if (__instance.m_character.IsPlayer() && __instance.m_weapon != null)
                {
                    var player = (Player) __instance.m_character;
                    var weaponDamage = __instance.GetWeapon()?.GetDamage();
                    if (player.HasActiveMagicEffect(MagicEffectType.FrostDamageAOE) && weaponDamage.HasValue && weaponDamage.Value.m_frost > 0)
                    {
                        Modified = true;

                        __instance.m_triggerEffect.m_effectPrefabs = __instance.m_triggerEffect.m_effectPrefabs.AddItem(new EffectList.EffectData {
                            m_prefab = EpicLoot.LoadAsset<GameObject>(FxPrefabName),
                            m_enabled = true
                        }).ToArray();

                        var colliderPrefab = EpicLoot.LoadAsset<GameObject>("IceSpikesCollider");

                        var transform = player.transform;
                        var attackOrigin = __instance.GetAttackOrigin();
                        var effectPosition = attackOrigin.position + Vector3.up * __instance.m_attackHeight + transform.forward * __instance.m_attackRange + transform.right * __instance.m_attackOffset;

                        var collider = Object.Instantiate(colliderPrefab, effectPosition, transform.rotation, attackOrigin);
                        
                        var damageController = collider.AddComponent<FrostAOEDamageController>();
                        var damage = __instance.m_weapon.GetDamage();
                        damage.Modify(__instance.m_damageMultiplier);
                        damage.Modify(player.GetRandomSkillFactor(__instance.m_weapon.m_shared.m_skillType));
                        damage.Modify(__instance.GetLevelDamageFactor());
                        damageController.SetDamage(damage, player);
                    }
                }

                return true;
            }

            public static void Postfix(Attack __instance)
            {
                if (Modified)
                {
                    __instance.m_triggerEffect.m_effectPrefabs = __instance.m_triggerEffect.m_effectPrefabs.Where(x => x.m_prefab.name != FxPrefabName).ToArray();
                    Modified = false;
                }
            }
        }
    }

    public class FrostAOEDamageController : MonoBehaviour
    {
        public HashSet<Character> Colliders { get; } = new HashSet<Character>();
        private Player _player;
        private float _frostDamage;

        [UsedImplicitly]
        private void OnTriggerEnter(Collider other)
        {
            var character = other.GetComponent<Character>();
            if (character != null && !Colliders.Contains(character))
            {
                Colliders.Add(character);
            }
        }

        [UsedImplicitly]
        private void OnTriggerExit(Collider other)
        {
            var character = other.GetComponent<Character>();
            if (character != null)
            {
                Colliders.Remove(character);
            }
        }

        public void SetDamage(HitData.DamageTypes damage, Player player)
        {
            _player = player;
            var aoeDamagePercent = player.GetTotalActiveMagicEffectValue(MagicEffectType.FrostDamageAOE, 0.01f);
            _frostDamage = damage.GetTotalDamage() * aoeDamagePercent;
        }

        public void Start()
        {
            StartCoroutine(DamageCoroutine());
        }

        private IEnumerator DamageCoroutine()
        {
            yield return new WaitForSeconds(0.3f);

            if (_player == null)
            {
                Destroy(gameObject);
                yield break;
            }

            foreach (var characterCollider in Colliders)
            {
                if (characterCollider == null || characterCollider == _player)
                {
                    continue;
                }

                var hitData = new HitData();
                hitData.m_point = characterCollider.transform.position;
                hitData.m_dir = (characterCollider.transform.position - _player.transform.position).normalized;
                hitData.m_damage.m_frost = _frostDamage;
                hitData.SetAttacker(_player);

                characterCollider.Damage(hitData);
            }

            Destroy(gameObject);
        }
    }
}
