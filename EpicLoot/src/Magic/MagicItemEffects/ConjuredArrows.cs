using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string ConjuredArrows = nameof(ConjuredArrows);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // ConjuredArrows (set items only, NoRoll). While it is active the wearer's bows never use real arrows: every
    // shot is a frost arrow conjured from eitr, adding value% of the bow's own damage as frost. Each shot costs
    // EitrCost eitr, cut by up to a third with the Elemental Magic skill as a staff's cost is. Reworked from
    // RustyLoot's MagicArrow, which fired a fixed-damage arrow only once the player ran out of real ones.
    //
    // The conjured arrow is a real ammo ItemData that never touches an inventory: Attack.UseAmmo hands a copy to
    // the attack, and FireProjectileBurst takes the projectile, its velocity and the ammo damage from it as it
    // would from a frost arrow. So the frost goes through the draw percentage, the skill roll and status effects
    // like an arrow's damage does. Its shared data is a copy of the frost arrow's, built once, so changing it
    // leaves real frost arrows alone; its damage is filled in by the GetDamage postfix from the bow being fired.
    //
    // The cost is an attack eitr cost added in GetAttackEitr, so vanilla checks and charges it as it does a
    // staff's, ModifyAttackEitrUse scales it and a triple shot pays it three times. HaveAmmo asks for it up front,
    // so a draw does not start without the eitr for it.
    //
    // The projectile is a copy of the frost arrow's with a brighter glow, registered in ZNetScene on every client
    // so others see it. Its own name also stops an archery target "returning" real frost arrows for it.
    public static class ConjuredArrows
    {
        // Tunable in this effect's Config block in magiceffects.json, under this key name.
        public const float DefaultEitrCost = 8f;
        private const string EitrCostKey = "EitrCost";

        private const float SkillCostReduction = 0.33f; // vanilla's attack-cost cut at skill 100
        private const string SourceArrow = "ArrowFrost";
        public const string ProjectileName = "EL_ConjuredArrow_projectile";
        private const string ArrowName = "$mod_epicloot_conjuredarrow";
        private const float EmissionBoost = 5f;
        private static readonly Color FallbackEmission = new Color(0.3f, 0.8f, 1f);

        private static GameObject _container;    // disabled parent that keeps the templates from Awaking
        private static GameObject _projectile;
        private static ItemDrop.ItemData _ammo;
        private static bool _missingLogged;

        private static float EitrCost => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.ConjuredArrows, EitrCostKey, DefaultEitrCost));

        // Tooltip: "... adding {0}% of the bow's damage as frost. Each shot costs {1} Eitr ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.ConjuredArrows, value => new object[] { value, EitrCost });
        }

        // Hooked to PrefabManager.OnPrefabsRegistered (a ZNetScene.Awake postfix on every client and the server,
        // each world load). The templates are built once and kept across worlds; later loads only re-inject.
        public static void RegisterPrefabs()
        {
            var zns = ZNetScene.instance;
            if (zns == null || !BuildTemplates(zns))
            {
                return;
            }

            if (!zns.m_prefabs.Contains(_projectile))
            {
                zns.m_prefabs.Add(_projectile);
            }
            zns.m_namedPrefabs[ProjectileName.GetStableHashCode()] = _projectile;
        }

        private static bool BuildTemplates(ZNetScene zns)
        {
            if (_projectile != null && _ammo != null)
            {
                return true;
            }

            var arrow = zns.GetPrefab(SourceArrow);
            var arrowDrop = arrow != null ? arrow.GetComponent<ItemDrop>() : null;
            var source = arrowDrop != null ? arrowDrop.m_itemData.m_shared.m_attack.m_attackProjectile : null;
            if (source == null)
            {
                if (!_missingLogged)
                {
                    _missingLogged = true;
                    EpicLoot.LogWarning($"ConjuredArrows: could not find the '{SourceArrow}' projectile; bows keep using real arrows.");
                }
                return false;
            }

            if (_container == null)
            {
                _container = new GameObject("EL_ConjuredArrowContainer");
                _container.SetActive(false);
                Object.DontDestroyOnLoad(_container);
            }

            // Under the disabled container nothing on the copies Awakes, while activeSelf stays true: remote
            // clients build the projectile from this template and never call SetActive on it.
            _projectile = Object.Instantiate(source, _container.transform);
            _projectile.name = ProjectileName;
            Brighten(_projectile);

            // Instantiate deep-copies the item's serialized shared data, so this ammo has a SharedData of its own.
            var ammoObject = Object.Instantiate(arrow, _container.transform);
            ammoObject.name = "EL_ConjuredArrow";
            _ammo = ammoObject.GetComponent<ItemDrop>().m_itemData;
            _ammo.m_shared.m_name = ArrowName;
            _ammo.m_shared.m_damages = new HitData.DamageTypes();
            _ammo.m_shared.m_damagesPerLevel = new HitData.DamageTypes();
            _ammo.m_shared.m_attack.m_attackProjectile = _projectile;
            _ammo.m_dropPrefab = null; // nothing to drop or give back (AmmoConservation bails on a null prefab)
            _ammo.m_stack = 1;
            return true;
        }

        // A stronger glow than the frost arrow it is copied from. Copies of the materials, so the frost arrow
        // itself is left as it is.
        private static void Brighten(GameObject projectile)
        {
            foreach (var renderer in projectile.GetComponentsInChildren<MeshRenderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty("_EmissionColor"))
                    {
                        continue;
                    }

                    var material = new Material(materials[i]) { name = materials[i].name + "_conjured" };
                    Color emission = material.GetColor("_EmissionColor");
                    if (emission.maxColorComponent <= 0.01f)
                    {
                        emission = FallbackEmission;
                    }
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission * EmissionBoost);
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static bool IsConjuring(Character character, ItemDrop.ItemData weapon)
        {
            return _ammo != null && character != null && character == Player.m_localPlayer &&
                   ElementalArchery.IsDrawnBow(weapon) &&
                   Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ConjuredArrows, out float _);
        }

        // The cost before ModifyAttackEitrUse and the like.
        private static float GetBaseShotCost(Character character)
        {
            return EitrCost * (1f - SkillCostReduction * character.GetSkillFactor(Skills.SkillType.ElementalMagic));
        }

        // What the shot costs once every modifier has had its say: the same call the attack makes.
        public static float GetShotCost(Character character, ItemDrop.ItemData weapon)
        {
            return weapon.m_shared.m_attack.GetAttackEitr(character, weapon);
        }

        // For the bow's tooltip: true, with the cost, when this bow would fire conjured arrows.
        public static bool TryGetShotCost(ItemDrop.ItemData weapon, out float cost)
        {
            cost = 0f;
            if (!IsConjuring(Player.m_localPlayer, weapon))
            {
                return false;
            }

            cost = GetShotCost(Player.m_localPlayer, weapon);
            return true;
        }

        private static float GetFrostDamage()
        {
            var player = Player.m_localPlayer;
            if (player == null || !player.HasActiveMagicEffect(MagicEffectType.ConjuredArrows, out float value))
            {
                return 0f;
            }

            var bow = MagicEffectsHelper.GetActiveWeapon(player);
            if (!ElementalArchery.IsDrawnBow(bow))
            {
                return 0f;
            }

            return bow.GetDamage().EpicLootGetTotalDamage() * value / 100f;
        }

        // Checked when a draw starts and again when the attack starts. Asks for the eitr instead of arrows, with
        // vanilla's own refusal (eitr bar flash, or "Eitr required" without eitr food).
        [HarmonyPatch(typeof(Attack), nameof(Attack.HaveAmmo))]
        private static class Attack_HaveAmmo_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
            {
                if (!IsConjuring(character, weapon))
                {
                    return true;
                }

                __result = character.TryUseEitr(GetShotCost(character, weapon));
                return false;
            }
        }

        // Vanilla equips arrows from the inventory here, and throws if there are none.
        [HarmonyPatch(typeof(Attack), nameof(Attack.EquipAmmoItem))]
        private static class Attack_EquipAmmoItem_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
            {
                if (!IsConjuring(character, weapon))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.UseAmmo))]
        private static class Attack_UseAmmo_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Attack __instance, ref ItemDrop.ItemData ammoItem, ref bool __result)
            {
                if (!IsConjuring(__instance.m_character, __instance.m_weapon))
                {
                    return true;
                }

                ammoItem = _ammo.Clone();
                __instance.m_ammoItem = ammoItem;

                // MultiShot clears these in its replacement of the arrow removal this skips.
                MultiShot.IsTripleShotActive = false;
                MultiShot.ShotProjectiles = 0;

                __result = true;
                return false;
            }
        }

        // Before ModifyAttackCosts (Normal priority) scales the total.
        [HarmonyPriority(Priority.HigherThanNormal)]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static class Attack_GetAttackEitr_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Character character, ItemDrop.ItemData weapon, ref float __result)
            {
                if (IsConjuring(character, weapon))
                {
                    __result += GetBaseShotCost(character);
                }
            }
        }

        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int), typeof(float))]
        private static class ItemData_GetDamage_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
            {
                if (_ammo != null && __instance.m_shared == _ammo.m_shared)
                {
                    __result = new HitData.DamageTypes { m_frost = GetFrostDamage() };
                }
            }
        }
    }
}
