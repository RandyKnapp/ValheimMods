using HarmonyLib;
using System.Globalization;
using UnityEngine;

namespace EpicLoot.Abilities
{
    // Ability cooldowns live on the player's ZDO, and vanilla spawns a new player (with a new ZDO) on every login and
    // after every death, so the seconds left are kept in the player's custom data across the swap. Like vanilla's
    // forsaken power cooldown, time spent logged out does not count. Vanilla's respawn saves the dead player before
    // destroying it, so a death in between keeps the time too.
    public static class AbilityCooldownSave
    {
        private static string SaveKey(string abilityID) => $"EpicLoot.{abilityID}.CooldownRemaining";

        // Covers every defined ability, not only the ones worn now: taking the set off keeps its cooldown on the ZDO.
        // An entry for an ability the config no longer defines is left as it is.
        private static void Store(Player player)
        {
            ZDO zdo = player.m_nview?.GetZDO();
            if (ZNet.instance == null || zdo == null)
            {
                return;
            }

            float now = (float)ZNet.instance.GetTimeSeconds();
            foreach (string abilityID in AbilityDefinitions.Abilities.Keys)
            {
                float left = zdo.GetFloat(Ability.CooldownEndKeyFor(abilityID)) - now;
                if (left > 0f)
                {
                    player.m_customData[SaveKey(abilityID)] = left.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    player.m_customData.Remove(SaveKey(abilityID));
                }
            }
        }

        // Capped at the ability's current cooldown, in case it was lowered since the save.
        private static void Restore(Player player)
        {
            // The character-select preview has no ZDO.
            ZDO zdo = player.m_nview?.GetZDO();
            if (ZNet.instance == null || zdo == null)
            {
                return;
            }

            float now = (float)ZNet.instance.GetTimeSeconds();
            foreach (AbilityDefinition abilityDef in AbilityDefinitions.Abilities.Values)
            {
                if (!player.m_customData.TryGetValue(SaveKey(abilityDef.ID), out string saved) ||
                    !float.TryParse(saved, NumberStyles.Float, CultureInfo.InvariantCulture, out float left))
                {
                    continue;
                }

                left = Mathf.Min(left, abilityDef.Cooldown);
                if (left > 0f)
                {
                    zdo.Set(Ability.CooldownEndKeyFor(abilityDef.ID), now + left);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Save))]
        public static class Player_Save_Patch
        {
            public static void Prefix(Player __instance)
            {
                Store(__instance);
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        public static class Player_Load_Patch
        {
            public static void Postfix(Player __instance)
            {
                Restore(__instance);
            }
        }
    }
}
