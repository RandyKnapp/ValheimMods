using System;
using System.Collections.Generic;

namespace EpicLoot.LegendarySystem
{
    /// <summary>
    /// What a player is wearing of one set. Pieces are counted once each (by LegendaryID) at the best
    /// rarity worn, so two copies of the same piece never count twice.
    /// </summary>
    public sealed class LegendarySetProgress
    {
        public LegendarySetInfo Set;
        /// <summary>Pieces needed for every bonus; see <see cref="UniqueLegendaryHelper.GetFullSetCount"/>.</summary>
        public int FullCount;
        /// <summary>Equipped piece ID to the highest rarity it is worn at.</summary>
        public readonly Dictionary<string, ItemRarity> EquippedPieces = new Dictionary<string, ItemRarity>();
        /// <summary>One entry per distinct equipped piece, highest rarity first.</summary>
        public readonly List<ItemRarity> PieceRarities = new List<ItemRarity>();

        public int Count => PieceRarities.Count;
        public bool IsFull => FullCount > 0 && Count >= FullCount;

        public bool IsActive(SetBonusInfo bonus)
        {
            return bonus != null && Count >= Math.Max(1, bonus.Count);
        }

        /// <summary>
        /// The rarity an active bonus applies at: the highest rarity that at least bonus.Count of the
        /// equipped pieces reach. Two Ancient pieces and one Legendary give a 2-piece bonus at Ancient and a
        /// 3-piece bonus at Legendary. Null while the bonus is inactive.
        /// </summary>
        public ItemRarity? GetTier(SetBonusInfo bonus)
        {
            return IsActive(bonus) ? PieceRarities[Math.Max(1, bonus.Count) - 1] : (ItemRarity?)null;
        }
    }

    /// <summary>
    /// The one place set progress and bonus values are worked out, shared by bonus application
    /// (PlayerExtensions), the set tooltip and the API, so they can never disagree.
    /// </summary>
    public static class SetBonusEvaluator
    {
        /// <summary>A bonus's value at <paramref name="tier"/>: its per-rarity override, else its flat
        /// Values, else <see cref="MagicItemEffect.DefaultValue"/> (value-less bonuses such as Undying).</summary>
        public static float GetBonusValue(SetBonusInfo bonus, ItemRarity tier)
        {
            return UniqueLegendaryHelper.ResolveValues(bonus?.Effect, tier)?.MinValue ?? MagicItemEffect.DefaultValue;
        }

        public static LegendarySetProgress GetSetProgress(Player player, LegendarySetInfo set)
        {
            return player == null ? CreateProgress(set) : GetSetProgress(set, player.GetMagicEquipment());
        }

        public static LegendarySetProgress GetSetProgress(LegendarySetInfo set, IEnumerable<ItemDrop.ItemData> equippedMagic)
        {
            LegendarySetProgress progress = CreateProgress(set);
            if (set == null || equippedMagic == null)
            {
                return progress;
            }

            int anonymous = 0;
            foreach (ItemDrop.ItemData item in equippedMagic)
            {
                if (item != null && item.IsMagic(out MagicItem magicItem) && magicItem.SetID == set.ID)
                {
                    AddPiece(progress, magicItem, ref anonymous);
                }
            }

            Finish(progress);
            return progress;
        }

        /// <summary>Progress for every set the player has at least one piece of, from one pass over their
        /// equipment.</summary>
        public static List<LegendarySetProgress> GetEquippedSetProgress(Player player)
        {
            return player == null ? new List<LegendarySetProgress>() : CollectSetProgress(player.GetMagicEquipment());
        }

        /// <summary>Progress for the set's looks only (SetAura, SetVisuals, SetArmorTint): the equipped items plus the
        /// hand items stowed on the back (R, Humanoid.HideHandItems), so putting a set weapon away keeps the look.
        /// Never use this for bonuses: a stowed item is unequipped, and its bonuses stop with it.</summary>
        public static List<LegendarySetProgress> GetVisibleSetProgress(Player player)
        {
            if (player == null)
            {
                return new List<LegendarySetProgress>();
            }

            List<ItemDrop.ItemData> items = player.GetMagicEquipment();
            items.Add(player.m_hiddenLeftItem);
            items.Add(player.m_hiddenRightItem);
            return CollectSetProgress(items);
        }

        private static List<LegendarySetProgress> CollectSetProgress(IEnumerable<ItemDrop.ItemData> items)
        {
            List<LegendarySetProgress> result = new List<LegendarySetProgress>();
            Dictionary<string, LegendarySetProgress> bySet = null;
            int anonymous = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.IsMagic(out MagicItem magicItem) || string.IsNullOrEmpty(magicItem.SetID))
                {
                    continue;
                }

                bySet ??= new Dictionary<string, LegendarySetProgress>();
                if (!bySet.TryGetValue(magicItem.SetID, out LegendarySetProgress progress))
                {
                    if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem.SetID, out LegendarySetInfo set))
                    {
                        continue;
                    }

                    progress = CreateProgress(set);
                    bySet.Add(magicItem.SetID, progress);
                    result.Add(progress);
                }

                AddPiece(progress, magicItem, ref anonymous);
            }

            foreach (LegendarySetProgress progress in result)
            {
                Finish(progress);
            }

            return result;
        }

        private static LegendarySetProgress CreateProgress(LegendarySetInfo set)
        {
            return new LegendarySetProgress
            {
                Set = set,
                FullCount = UniqueLegendaryHelper.GetFullSetCount(set)
            };
        }

        private static void AddPiece(LegendarySetProgress progress, MagicItem magicItem, ref int anonymous)
        {
            // An item carrying a SetID without a LegendaryID (hand-written through the API) is its own piece.
            string key = string.IsNullOrEmpty(magicItem.LegendaryID) ? $"<unnamed {anonymous++}>" : magicItem.LegendaryID;
            if (!progress.EquippedPieces.TryGetValue(key, out ItemRarity best) || magicItem.Rarity > best)
            {
                progress.EquippedPieces[key] = magicItem.Rarity;
            }
        }

        private static void Finish(LegendarySetProgress progress)
        {
            progress.PieceRarities.Clear();
            progress.PieceRarities.AddRange(progress.EquippedPieces.Values);
            progress.PieceRarities.Sort((a, b) => b.CompareTo(a));
        }
    }
}
