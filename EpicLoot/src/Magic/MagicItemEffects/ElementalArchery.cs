using HarmonyLib;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string ElementalArchery = nameof(ElementalArchery);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // ElementalArchery (set items only, NoRoll, no value). The wearer's bows use the Elemental Magic skill in place
    // of Bows: damage, draw speed, draw and attack stamina, and the tooltip's damage range all follow Elemental
    // Magic, and a bow hit (on a creature or an archery target) trains Elemental Magic instead of Bows.
    //
    // The bow's m_skillType is shared by every copy of that bow, so it is never changed. Instead the Skills calls
    // those readers end in are remapped, and only while one of them is running for a drawn bow of the local
    // player's: a scope counter opened by a prefix and closed by a finalizer around each reader, and prefixes on
    // Skills.GetSkillFactor / GetSkillLevel / RaiseSkill that turn Bows into ElementalMagic while it is open.
    // EpicLoot's AddSkillLevel routes GetRandomSkillFactor/Range through GetSkillFactor, so the damage roll is
    // covered by the GetSkillFactor remap. Outside a scope nothing changes, so the Skills dialog still shows the
    // real Bows level.
    //
    // Deliberately left as Bows: the hit's m_skill (stagger tagging in ModifyStaggerDuration, kill statistics)
    // and status effects that modify bow attacks, which key on the weapon's skill field.
    public static class ElementalArchery
    {
        private const string GrapplingHookName = "$item_graplinghook";

        private static int _scopeDepth;

        // True while a remapped bow read is running, so Interconnected treats the held bow as Elemental Magic too.
        internal static bool IsRemapping => _scopeDepth > 0;

        // A bow drawn and loosed with arrows; not a crossbow, staff or the grappling hook.
        public static bool IsDrawnBow(ItemDrop.ItemData item)
        {
            return item != null &&
                   item.m_shared.m_skillType == Skills.SkillType.Bows &&
                   item.m_shared.m_attack.m_bowDraw &&
                   item.m_shared.m_name != GrapplingHookName;
        }

        private static bool IsActiveFor(Character character, ItemDrop.ItemData weapon)
        {
            return character != null && character == Player.m_localPlayer && IsDrawnBow(weapon) &&
                   Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ElementalArchery, out float _);
        }

        private static void Enter(bool active, out bool entered)
        {
            entered = active;
            if (active)
            {
                _scopeDepth++;
            }
        }

        private static void Exit(bool entered)
        {
            if (entered)
            {
                _scopeDepth--;
            }
        }

        private static void Remap(Skills skills, ref Skills.SkillType skillType)
        {
            if (_scopeDepth > 0 && skillType == Skills.SkillType.Bows && skills.m_player != null &&
                skills.m_player == Player.m_localPlayer)
            {
                skillType = Skills.SkillType.ElementalMagic;
            }
        }

        // Priority.First, so the other prefixes on these methods (AddSkillLevel's range and factor, the XP
        // multipliers on RaiseSkill, some of them skill-gated) already see Elemental Magic.
        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
        private static class Skills_GetSkillFactor_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Skills __instance, ref Skills.SkillType skillType)
            {
                Remap(__instance, ref skillType);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
        private static class Skills_GetSkillLevel_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Skills __instance, ref Skills.SkillType skillType)
            {
                Remap(__instance, ref skillType);
            }
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class Skills_RaiseSkill_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(Skills __instance, ref Skills.SkillType skillType)
            {
                Remap(__instance, ref skillType);
            }
        }

        // Draw speed.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
        private static class Humanoid_GetAttackDrawPercentage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Humanoid __instance, out bool __state) =>
                Enter(__instance is Player && IsActiveFor(__instance, __instance.GetCurrentWeapon()), out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }

        // Stamina drained while holding the draw.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
        private static class ItemData_GetDrawStaminaDrain_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(ItemDrop.ItemData __instance, out bool __state) =>
                Enter(IsActiveFor(Player.m_localPlayer, __instance), out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
        private static class Attack_GetAttackStamina_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Attack __instance, out bool __state) =>
                Enter(IsActiveFor(__instance.m_character, __instance.m_weapon), out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }

        // The damage roll (GetRandomSkillFactor) and the hit's skill level.
        [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
        private static class Attack_FireProjectileBurst_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Attack __instance, out bool __state) =>
                Enter(IsActiveFor(__instance.m_character, __instance.m_weapon), out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }

        // The skill raise for a hit: vanilla raises the projectile's m_skill on a creature it damages, and an
        // archery target raises it from inside OnHit too. Read when the arrow lands, so taking the set off while
        // it flies leaves the experience with Bows.
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private static class Projectile_OnHit_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, out bool __state) =>
                Enter(__instance.m_skill == Skills.SkillType.Bows && IsActiveFor(__instance.m_owner, __instance.m_weapon),
                    out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }

        // The tooltip's damage range and skill level, vanilla's and EpicLoot's alike (MagicTooltip runs inside
        // this method). Priority.First so the scope is open before EpicLoot's tooltip prefix builds the text.
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetTooltip),
            typeof(ItemDrop.ItemData), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(bool))]
        private static class ItemData_GetTooltip_Patch
        {
            [HarmonyPrefix]
            [HarmonyPriority(Priority.First)]
            private static void Prefix(ItemDrop.ItemData item, out bool __state) =>
                Enter(IsActiveFor(Player.m_localPlayer, item), out __state);

            [HarmonyFinalizer]
            private static void Finalizer(bool __state) => Exit(__state);
        }
    }
}
