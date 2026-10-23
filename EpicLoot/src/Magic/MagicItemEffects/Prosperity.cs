using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Magic.MagicItemEffects
{
    // Prosperity: a chance for a creature killed, or a chest opened, near the player to roll its whole
    // EpicLoot loot table again. Luck tilts which rarity a roll lands on; Prosperity adds rolls.
    //
    // The value is a percentage. Every full 100 is a guaranteed extra roll and the remainder is the chance
    // of one more, so the expected number of EpicLoot drops scales by (1 + value / 100), up to the
    // BonusRollsMax cap. LootRoller.RollLootTableInternal spends the rolls; Lucky Loot's bonus rolls and
    // every cheat or gamble roll opt out there.
    public static class Prosperity
    {
        public const int DefaultBonusRollsMax = 3;

        private const string BonusRollsMaxKey = "BonusRollsMax";

        // Matches Luck. The effect's owner has to be in the area for the roll to count.
        private const float PlayerScanRange = 100f;

        // Player-ZDO key holding the local player's Prosperity value, mirrored by
        // Multiplayer_Player_Patch.UpdateRichesAndLuck on equip/unequip. A float, as Luck's and Riches' keys
        // are, so a fractional value is not truncated.
        public const string ZdoValueKey = "el-prs";

        // Loot rolls on whichever machine owns the creature or chest -- possibly a dedicated server with
        // no local player -- so, like Luck, the value is read off nearby players' ZDOs rather than from
        // Player.m_localPlayer.
        //
        // Summed across players, as Luck is. BonusRollsMax bounds what a group can reach.
        public static float GetFactor(Vector3 position)
        {
            var players = new List<Player>();
            Player.GetPlayersInRange(position, PlayerScanRange, players);

            var total = 0f;
            foreach (var player in players)
            {
                var zdo = player?.m_nview?.GetZDO();
                if (zdo == null)
                {
                    continue;
                }

                total += zdo.GetFloat(ZdoValueKey);
            }

            return total * 0.01f;
        }

        // How many extra times the loot at this position is rolled.
        public static int RollBonusRolls(Vector3 position)
        {
            var factor = GetFactor(position);
            if (factor <= 0)
            {
                return 0;
            }

            var guaranteed = Mathf.FloorToInt(factor);
            var rolls = guaranteed + (Random.value < factor - guaranteed ? 1 : 0);
            return Mathf.Clamp(rolls, 0, GetBonusRollsMax());
        }

        private static int GetBonusRollsMax()
        {
            var cfg = MagicItemEffectDefinitions.GetEffectConfig(MagicEffectType.Prosperity);
            if (cfg != null && cfg.TryGetValue(BonusRollsMaxKey, out var raw))
            {
                return Mathf.Max(0, Mathf.RoundToInt(raw));
            }

            return DefaultBonusRollsMax;
        }
    }
}
