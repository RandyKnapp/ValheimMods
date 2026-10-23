using Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.LegendarySystem
{
    /// <summary>
    /// Every unique ("legendary") and item set from legendaries.json, in one pool whatever rarity it rolls
    /// at. Each entry carries a Rarities list; the legacy per-rarity blocks (LegendaryItems vs MythicItems)
    /// only decide the default when that list is empty. A unique defaults to its own block's rarity, and a
    /// set to the rarity of the blocks its pieces are in, which is where the old per-pool roll dropped them.
    /// A set piece always rolls at its set's rarities.
    /// </summary>
    public static class UniqueLegendaryHelper
    {
        internal static LegendaryItemConfig Config;

        /// <summary>Every unique, set pieces included, keyed by ID.</summary>
        public static readonly Dictionary<string, LegendaryInfo> AllUniques = new Dictionary<string, LegendaryInfo>();
        /// <summary>Every set, keyed by ID.</summary>
        public static readonly Dictionary<string, LegendarySetInfo> AllSets = new Dictionary<string, LegendarySetInfo>();
        /// <summary>Set piece ID to the set that lists it.</summary>
        public static readonly Dictionary<string, LegendarySetInfo> ItemsToSetMap = new Dictionary<string, LegendarySetInfo>();

        // Legacy names, kept for mods that read them. They are the unified collections above, so they now
        // hold every unique and set, not just the Legendary ones. Declared after them: static fields
        // initialize in source order.
        public static readonly Dictionary<string, LegendaryInfo> LegendaryInfo = AllUniques;
        public static readonly Dictionary<string, LegendarySetInfo> LegendarySets = AllSets;
        public static readonly Dictionary<string, LegendarySetInfo> LegendaryItemsToSetMap = ItemsToSetMap;

        // Legacy read-only views of the entries enabled at Mythic, rebuilt on every change. Writing to them
        // registers nothing; use API.AddLegendaryItem / AddLegendarySet.
        public static readonly Dictionary<string, LegendaryInfo> MythicInfo = new Dictionary<string, LegendaryInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> MythicSets = new Dictionary<string, LegendarySetInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> MythicItemsToSetMap = new Dictionary<string, LegendarySetInfo>();

        public static readonly LegendaryInfo GenericLegendaryInfo = new LegendaryInfo
        {
            ID = nameof(GenericLegendaryInfo)
        };

        public static event Action OnSetupLegendaryItemConfig;

        private static readonly List<ItemRarity> DefaultRarities = new List<ItemRarity> { ItemRarity.Legendary };
        // The JSON name of LegendaryInfo/LegendarySetInfo.Rarities. Not nameof: inside this class the name
        // LegendaryInfo binds to the legacy dictionary field, not the type.
        private const string RaritiesField = "Rarities";

        // Problems found while building the pool, reported by Validate. Messages already logged for the
        // current config are remembered so an API registration's rebuild does not repeat them.
        private static readonly List<string> _buildIssues = new List<string>();
        private static readonly HashSet<string> _reportedIssues = new HashSet<string>();
        // Off until every mod has had the chance to register through the API, so a set whose pieces
        // another mod adds is not reported as broken during EpicLoot's own Awake.
        private static bool _validationEnabled;
        // Rarity lists this class copied onto set pieces, so a later rebuild can tell an inherited list
        // (which may lag a set whose rarities an API update changed) from one the author wrote.
        private static readonly HashSet<List<ItemRarity>> _inheritedPieceRarities = new HashSet<List<ItemRarity>>();

        public static void Initialize(LegendaryItemConfig config)
        {
            if (config == null)
            {
                EpicLoot.LogWarning("UniqueLegendaryHelper.Initialize called with a null config; keeping the currently loaded legendaries.");
                return;
            }

            Config = config;
            _reportedIssues.Clear();
            _inheritedPieceRarities.Clear();
            OnSetupLegendaryItemConfig?.Invoke();
            RefreshDerived();
        }

        public static LegendaryItemConfig GetCFG()
        {
            return Config;
        }

        /// <summary>
        /// Called once every mod has registered its items (ItemManager.OnItemsRegistered): reports the
        /// problems in the loaded config, and does so on every later rebuild.
        /// </summary>
        internal static void EnableValidation()
        {
            _validationEnabled = true;
            Validate();
        }

        /// <summary>
        /// Rebuilds the pool from <see cref="Config"/>. Idempotent: it only fills rarity lists that are
        /// empty and merges duplicates into the first copy, so a client re-initializing from the copy the
        /// server sent (which already carries every backfilled list) changes nothing.
        /// </summary>
        internal static void RefreshDerived()
        {
            Config ??= new LegendaryItemConfig();

            AllSets.Clear();
            ItemsToSetMap.Clear();
            AllUniques.Clear();
            _buildIssues.Clear();

            // Mythic blocks first, matching the old lookups, which checked the Mythic pool first: an ID
            // defined in both blocks keeps resolving to the definition it did before.
            Dictionary<string, List<ItemRarity>> pieceBlocks = GetBlockRarities();
            AddSets(Config.MythicSets, ItemRarity.Mythic, pieceBlocks);
            AddSets(Config.LegendarySets, ItemRarity.Legendary, pieceBlocks);
            AddUniques(Config.MythicItems, ItemRarity.Mythic);
            AddUniques(Config.LegendaryItems, ItemRarity.Legendary);

            foreach (KeyValuePair<string, LegendarySetInfo> piece in ItemsToSetMap)
            {
                if (AllUniques.TryGetValue(piece.Key, out LegendaryInfo info))
                {
                    ApplySetRarities(info, piece.Value);
                }
            }

            RebuildMythicViews();
            EquipmentEffectCache.ResetAll();

            if (_validationEnabled)
            {
                Validate();
            }
        }

        // The rarity of the block each unique is defined in (both, when an ID is in both). Before sets had a
        // Rarities list, a piece dropped from its own block's pool whichever block its set was in.
        private static Dictionary<string, List<ItemRarity>> GetBlockRarities()
        {
            Dictionary<string, List<ItemRarity>> result = new Dictionary<string, List<ItemRarity>>();

            void Collect(List<LegendaryInfo> uniques, ItemRarity rarity)
            {
                if (uniques == null)
                {
                    return;
                }

                foreach (LegendaryInfo info in uniques)
                {
                    if (info == null || string.IsNullOrEmpty(info.ID))
                    {
                        continue;
                    }

                    if (!result.TryGetValue(info.ID, out List<ItemRarity> rarities))
                    {
                        rarities = new List<ItemRarity>();
                        result.Add(info.ID, rarities);
                    }

                    if (!rarities.Contains(rarity))
                    {
                        rarities.Add(rarity);
                    }
                }
            }

            Collect(Config.MythicItems, ItemRarity.Mythic);
            Collect(Config.LegendaryItems, ItemRarity.Legendary);
            return result;
        }

        // What a set with no Rarities of its own rolls at: the blocks its pieces are in, or, when none of
        // its pieces is defined, the set's own block.
        private static List<ItemRarity> GetDefaultSetRarities(LegendarySetInfo set, ItemRarity setBlockRarity,
            Dictionary<string, List<ItemRarity>> pieceBlocks)
        {
            List<ItemRarity> result = new List<ItemRarity>();
            foreach (string pieceID in set.LegendaryIDs)
            {
                if (!string.IsNullOrEmpty(pieceID) && pieceBlocks.TryGetValue(pieceID, out List<ItemRarity> rarities))
                {
                    foreach (ItemRarity rarity in rarities)
                    {
                        if (!result.Contains(rarity))
                        {
                            result.Add(rarity);
                        }
                    }
                }
            }

            if (result.Count == 0)
            {
                result.Add(setBlockRarity);
            }

            return result;
        }

        private static void AddSets(List<LegendarySetInfo> sets, ItemRarity defaultRarity,
            Dictionary<string, List<ItemRarity>> pieceBlocks)
        {
            if (sets == null)
            {
                return;
            }

            foreach (LegendarySetInfo set in sets)
            {
                if (set == null || string.IsNullOrEmpty(set.ID))
                {
                    _buildIssues.Add("A set in legendaries.json has no ID and was skipped.");
                    continue;
                }

                set.LegendaryIDs ??= new List<string>();
                set.SetBonuses ??= new List<SetBonusInfo>();
                set.SetBonuses.RemoveAll(x => x == null);
                set.Rarities = NormalizeRarities(set.Rarities,
                    GetDefaultSetRarities(set, defaultRarity, pieceBlocks), $"set '{set.ID}'");

                if (AllSets.TryGetValue(set.ID, out LegendarySetInfo existing))
                {
                    if (!ReferenceEquals(existing, set))
                    {
                        MergeDuplicate(set.ID, existing, set, existing.Rarities, set.Rarities, "set");
                    }

                    continue;
                }

                AllSets.Add(set.ID, set);
                foreach (string pieceID in set.LegendaryIDs)
                {
                    if (string.IsNullOrEmpty(pieceID))
                    {
                        continue;
                    }

                    if (ItemsToSetMap.TryGetValue(pieceID, out LegendarySetInfo owner))
                    {
                        if (owner != set)
                        {
                            _buildIssues.Add($"'{pieceID}' is listed by both set '{owner.ID}' and set '{set.ID}'; " +
                                $"it belongs to '{owner.ID}'. A piece can only be in one set.");
                        }

                        continue;
                    }

                    ItemsToSetMap.Add(pieceID, set);
                }
            }
        }

        private static void AddUniques(List<LegendaryInfo> uniques, ItemRarity defaultRarity)
        {
            if (uniques == null)
            {
                return;
            }

            foreach (LegendaryInfo info in uniques)
            {
                if (info == null || string.IsNullOrEmpty(info.ID))
                {
                    _buildIssues.Add("A unique in legendaries.json has no ID and was skipped.");
                    continue;
                }

                info.Requirements ??= new MagicItemEffectRequirements();
                info.GuaranteedMagicEffects ??= new List<GuaranteedMagicEffect>();

                // A set piece's list is replaced by its set's afterwards, so leave it alone here.
                if (!ItemsToSetMap.ContainsKey(info.ID))
                {
                    info.Rarities = NormalizeRarities(info.Rarities, new List<ItemRarity> { defaultRarity },
                        $"unique '{info.ID}'");
                }

                if (AllUniques.TryGetValue(info.ID, out LegendaryInfo existing))
                {
                    if (!ReferenceEquals(existing, info))
                    {
                        MergeDuplicate(info.ID, existing, info, existing.Rarities, info.Rarities, "unique");
                    }

                    continue;
                }

                AllUniques.Add(info.ID, info);
            }
        }

        // The same ID in two blocks (RelicHeim-style per-rarity copies) becomes one entry enabled at the
        // rarities of both. Only a real difference in the definition is worth a warning.
        private static void MergeDuplicate(string id, object kept, object duplicate, List<ItemRarity> keptRarities,
            List<ItemRarity> duplicateRarities, string kind)
        {
            if (keptRarities != null && duplicateRarities != null)
            {
                foreach (ItemRarity rarity in duplicateRarities)
                {
                    if (!keptRarities.Contains(rarity))
                    {
                        keptRarities.Add(rarity);
                    }
                }

                keptRarities.Sort();
            }

            if (!DefinitionsMatch(kept, duplicate))
            {
                _buildIssues.Add($"The {kind} '{id}' is defined more than once with different contents; " +
                    $"the first definition (MythicItems/MythicSets before LegendaryItems/LegendarySets) is used " +
                    $"at every rarity. Define it once and list its rarities in \"Rarities\".");
            }
        }

        private static bool DefinitionsMatch(object a, object b)
        {
            try
            {
                JObject left = JObject.FromObject(a);
                JObject right = JObject.FromObject(b);
                left.Remove(RaritiesField);
                right.Remove(RaritiesField);
                return JToken.DeepEquals(left, right);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ApplySetRarities(LegendaryInfo piece, LegendarySetInfo set)
        {
            List<ItemRarity> own = piece.Rarities;
            if (own != null && own.Count > 0 && !_inheritedPieceRarities.Contains(own) &&
                !own.SequenceEqual(set.Rarities))
            {
                _buildIssues.Add($"Set piece '{piece.ID}' lists its own Rarities; set pieces roll at their set's " +
                    $"({string.Join(", ", set.Rarities)}, from '{set.ID}'). Narrow a single piece with " +
                    $"Requirements.AllowedRarities instead.");
            }

            if (own != null)
            {
                _inheritedPieceRarities.Remove(own);
            }

            piece.Rarities = new List<ItemRarity>(set.Rarities);
            _inheritedPieceRarities.Add(piece.Rarities);
        }

        // Null becomes empty, values outside the enum are dropped, duplicates removed, and an empty list
        // gets the defaults.
        private static List<ItemRarity> NormalizeRarities(List<ItemRarity> rarities, List<ItemRarity> defaults, string owner)
        {
            List<ItemRarity> result = new List<ItemRarity>();
            if (rarities != null)
            {
                foreach (ItemRarity rarity in rarities)
                {
                    if (!Enum.IsDefined(typeof(ItemRarity), rarity))
                    {
                        _buildIssues.Add($"The {owner} lists an unknown rarity ({(int)rarity}); it was ignored.");
                        continue;
                    }

                    if (!result.Contains(rarity))
                    {
                        result.Add(rarity);
                    }
                }
            }

            if (result.Count == 0)
            {
                result.AddRange(defaults);
            }

            result.Sort();
            return result;
        }

        private static void RebuildMythicViews()
        {
            MythicInfo.Clear();
            MythicSets.Clear();
            MythicItemsToSetMap.Clear();

            foreach (LegendaryInfo info in AllUniques.Values)
            {
                if (IsEnabledAt(info, ItemRarity.Mythic))
                {
                    MythicInfo[info.ID] = info;
                }
            }

            foreach (LegendarySetInfo set in AllSets.Values)
            {
                if (IsEnabledAt(set, ItemRarity.Mythic))
                {
                    MythicSets[set.ID] = set;
                }
            }

            foreach (KeyValuePair<string, LegendarySetInfo> piece in ItemsToSetMap)
            {
                if (MythicSets.ContainsKey(piece.Value.ID))
                {
                    MythicItemsToSetMap[piece.Key] = piece.Value;
                }
            }
        }

        private static void Validate()
        {
            List<string> issues = new List<string>(_buildIssues);

            foreach (LegendarySetInfo set in AllSets.Values)
            {
                int pieceCount = set.LegendaryIDs.Where(id => !string.IsNullOrEmpty(id)).Distinct().Count();
                List<string> missing = set.LegendaryIDs
                    .Where(id => !string.IsNullOrEmpty(id) && !AllUniques.ContainsKey(id))
                    .Distinct()
                    .ToList();
                if (missing.Count > 0)
                {
                    issues.Add($"Set '{set.ID}' lists {string.Join(", ", missing.Select(id => $"'{id}'"))}, " +
                        $"which {(missing.Count == 1 ? "is" : "are")} not defined in LegendaryItems and can never drop.");
                }

                foreach (SetBonusInfo bonus in set.SetBonuses)
                {
                    if (bonus?.Effect == null || string.IsNullOrEmpty(bonus.Effect.Type))
                    {
                        issues.Add($"Set '{set.ID}' has a bonus with no effect type; it is ignored.");
                    }
                    else if (bonus.Count <= 0)
                    {
                        issues.Add($"Set '{set.ID}' bonus {bonus.Effect.Type} needs {bonus.Count} pieces; " +
                            $"a bonus needs at least 1.");
                    }
                    else if (bonus.Count > pieceCount)
                    {
                        issues.Add($"Set '{set.ID}' bonus {bonus.Effect.Type} needs {bonus.Count} pieces but the " +
                            $"set only has {pieceCount}; it can never activate.");
                    }
                }
            }

            foreach (LegendaryInfo info in AllUniques.Values)
            {
                bool listed = ItemsToSetMap.ContainsKey(info.ID);
                if (info.IsSetItem && !listed)
                {
                    issues.Add($"'{info.ID}' has IsSetItem but no set lists it; it rolls as a regular unique.");
                }
                else if (!info.IsSetItem && listed)
                {
                    issues.Add($"'{info.ID}' is listed by set '{ItemsToSetMap[info.ID].ID}' but does not have " +
                        $"IsSetItem; it is treated as a set piece.");
                }
            }

            foreach (string issue in issues)
            {
                if (_reportedIssues.Add(issue))
                {
                    EpicLoot.LogWarning($"[legendaries.json] {issue}");
                }
            }
        }

        /// <summary>Adds a unique registered at runtime (API). An empty Rarities list gets
        /// <paramref name="defaultRarity"/>.</summary>
        internal static void RegisterUnique(LegendaryInfo info, ItemRarity defaultRarity)
        {
            Config ??= new LegendaryItemConfig();
            if (info.Rarities == null || info.Rarities.Count == 0)
            {
                info.Rarities = new List<ItemRarity> { defaultRarity };
            }

            Config.LegendaryItems.Add(info);
            RefreshDerived();
        }

        /// <summary>Adds a set registered at runtime (API). An empty Rarities list gets
        /// <paramref name="defaultRarity"/>.</summary>
        internal static void RegisterSet(LegendarySetInfo set, ItemRarity defaultRarity)
        {
            Config ??= new LegendaryItemConfig();
            if (set.Rarities == null || set.Rarities.Count == 0)
            {
                set.Rarities = new List<ItemRarity> { defaultRarity };
            }

            Config.LegendarySets.Add(set);
            RefreshDerived();
        }

        public static bool TryGetLegendaryInfo(string legendaryID, out LegendaryInfo legendaryInfo)
        {
            if (string.IsNullOrEmpty(legendaryID))
            {
                legendaryInfo = null;
                return false;
            }

            return AllUniques.TryGetValue(legendaryID, out legendaryInfo);
        }

        public static bool IsGenericLegendary(LegendaryInfo legendaryInfo)
        {
            return legendaryInfo == GenericLegendaryInfo;
        }

        public static bool IsSetPiece(LegendaryInfo legendaryInfo)
        {
            return legendaryInfo != null && !string.IsNullOrEmpty(legendaryInfo.ID) &&
                ItemsToSetMap.ContainsKey(legendaryInfo.ID);
        }

        /// <summary>The rarities this unique rolls at: its set's for a set piece.</summary>
        public static List<ItemRarity> GetRarities(LegendaryInfo legendaryInfo)
        {
            if (legendaryInfo == null)
            {
                return new List<ItemRarity>();
            }

            if (!string.IsNullOrEmpty(legendaryInfo.ID) && ItemsToSetMap.TryGetValue(legendaryInfo.ID, out LegendarySetInfo set))
            {
                return GetRarities(set);
            }

            // An entry another mod wrote straight into the dictionary never went through the backfill.
            return legendaryInfo.Rarities?.Count > 0 ? legendaryInfo.Rarities : DefaultRarities;
        }

        public static List<ItemRarity> GetRarities(LegendarySetInfo setInfo)
        {
            if (setInfo == null)
            {
                return new List<ItemRarity>();
            }

            return setInfo.Rarities?.Count > 0 ? setInfo.Rarities : DefaultRarities;
        }

        public static bool IsEnabledAt(LegendaryInfo legendaryInfo, ItemRarity rarity)
        {
            return GetRarities(legendaryInfo).Contains(rarity);
        }

        public static bool IsEnabledAt(LegendarySetInfo setInfo, ItemRarity rarity)
        {
            return GetRarities(setInfo).Contains(rarity);
        }

        /// <summary>Whether any unique that is not a set piece rolls at <paramref name="rarity"/>.</summary>
        public static bool AnyUniqueEnabledAt(ItemRarity rarity)
        {
            foreach (LegendaryInfo info in AllUniques.Values)
            {
                if (!IsSetPiece(info) && IsEnabledAt(info, rarity))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool AnySetEnabledAt(ItemRarity rarity)
        {
            foreach (LegendarySetInfo set in AllSets.Values)
            {
                if (IsEnabledAt(set, rarity))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every unique ID, set pieces included, that rolls at <paramref name="rarity"/>.</summary>
        public static List<string> GetUniquesEnabledAt(ItemRarity rarity)
        {
            return AllUniques.Values.Where(x => IsEnabledAt(x, rarity)).Select(x => x.ID).ToList();
        }

        /// <summary>
        /// The non-set uniques that can roll on <paramref name="baseItem"/> at <paramref name="rarity"/>,
        /// plus <see cref="GenericLegendaryInfo"/> (a unique-less item of that rarity).
        /// </summary>
        public static List<LegendaryInfo> GetAvailableUniques(ItemDrop.ItemData baseItem, MagicItem magicItem, ItemRarity rarity)
        {
            List<LegendaryInfo> result = AllUniques.Values
                .Where(x => !IsSetPiece(x) && IsEnabledAt(x, rarity) &&
                    (x.Requirements == null || x.Requirements.CheckRequirements(baseItem, magicItem)))
                .ToList();
            result.Add(GenericLegendaryInfo);
            return result;
        }

        /// <summary>
        /// The set pieces that can roll on <paramref name="baseItem"/> at <paramref name="rarity"/>. May be
        /// empty; there is no generic entry.
        /// </summary>
        public static List<LegendaryInfo> GetAvailableSetPieces(ItemDrop.ItemData baseItem, MagicItem magicItem, ItemRarity rarity)
        {
            List<LegendaryInfo> result = new List<LegendaryInfo>();
            foreach (KeyValuePair<string, LegendarySetInfo> piece in ItemsToSetMap)
            {
                if (!IsEnabledAt(piece.Value, rarity) || !AllUniques.TryGetValue(piece.Key, out LegendaryInfo info))
                {
                    continue;
                }

                if (info.Requirements == null || info.Requirements.CheckRequirements(baseItem, magicItem))
                {
                    result.Add(info);
                }
            }

            return result;
        }

        /// <summary>A set's pieces that are actually defined, once each, in the set's own order.</summary>
        public static IEnumerable<LegendaryInfo> GetDefinedSetPieces(LegendarySetInfo set)
        {
            if (set?.LegendaryIDs == null)
            {
                yield break;
            }

            foreach (string pieceID in set.LegendaryIDs.Where(id => !string.IsNullOrEmpty(id)).Distinct())
            {
                if (AllUniques.TryGetValue(pieceID, out LegendaryInfo info))
                {
                    yield return info;
                }
            }
        }

        /// <summary>
        /// The piece of <paramref name="set"/> that <paramref name="item"/> would become at
        /// <paramref name="rarity"/>, judged by each piece's Requirements the way a loot roll judges them.
        /// <paramref name="preferredID"/> is tried first when it is still one of the set's pieces, then the
        /// rest in the set's order. False when the set is not enabled at that rarity or no piece fits.
        /// </summary>
        public static bool TryGetSetPieceForItem(LegendarySetInfo set, ItemDrop.ItemData item, ItemRarity rarity,
            string preferredID, out LegendaryInfo piece)
        {
            piece = null;
            if (set == null || item == null || !IsEnabledAt(set, rarity))
            {
                return false;
            }

            // A fresh item of the target rarity, as LootRoller.RollMagicItem passes: the pieces' rules are
            // about the host item, and the item's own effects must not trip the effect-composition half.
            MagicItem probe = new MagicItem { Rarity = rarity };
            List<LegendaryInfo> pieces = GetDefinedSetPieces(set).ToList();

            LegendaryInfo preferred = string.IsNullOrEmpty(preferredID) ? null : pieces.Find(x => x.ID == preferredID);
            if (preferred != null && PieceFits(preferred, item, probe))
            {
                piece = preferred;
                return true;
            }

            foreach (LegendaryInfo candidate in pieces)
            {
                if (candidate != preferred && PieceFits(candidate, item, probe))
                {
                    piece = candidate;
                    return true;
                }
            }

            return false;
        }

        private static bool PieceFits(LegendaryInfo piece, ItemDrop.ItemData item, MagicItem probe)
        {
            return piece.Requirements == null || piece.Requirements.CheckRequirements(item, probe);
        }

        [Obsolete("Use GetAvailableUniques or GetAvailableSetPieces with a rarity.")]
        public static IList<LegendaryInfo> GetAvailableLegendaries(ItemDrop.ItemData baseItem, MagicItem magicItem, bool rollSetItem)
        {
            return GetAvailableLegacy(baseItem, magicItem, rollSetItem, ItemRarity.Legendary);
        }

        [Obsolete("Use GetAvailableUniques or GetAvailableSetPieces with a rarity.")]
        public static IList<LegendaryInfo> GetAvailableMythics(ItemDrop.ItemData baseItem, MagicItem magicItem, bool rollSetItem)
        {
            return GetAvailableLegacy(baseItem, magicItem, rollSetItem, ItemRarity.Mythic);
        }

        private static IList<LegendaryInfo> GetAvailableLegacy(ItemDrop.ItemData baseItem, MagicItem magicItem,
            bool rollSetItem, ItemRarity rarity)
        {
            if (!rollSetItem)
            {
                return GetAvailableUniques(baseItem, magicItem, rarity);
            }

            List<LegendaryInfo> pieces = GetAvailableSetPieces(baseItem, magicItem, rarity);
            return pieces.Count > 0 ? pieces : new List<LegendaryInfo> { GenericLegendaryInfo };
        }

        /// <summary>The value range an effect rolls with at <paramref name="rarity"/>: its per-rarity
        /// override, else its flat Values. Null when the entry declares neither.</summary>
        public static MagicItemEffectDefinition.ValueDef ResolveValues(GuaranteedMagicEffect effect, ItemRarity rarity)
        {
            if (effect == null)
            {
                return null;
            }

            return effect.ValuesPerRarity?.GetValueDefForRarity(rarity) ?? effect.Values;
        }

        public static MagicItemEffectDefinition.ValueDef GetLegendaryEffectValues(string legendaryID, string effectType, ItemRarity rarity)
        {
            if (TryGetLegendaryInfo(legendaryID, out LegendaryInfo info) &&
                info.GuaranteedMagicEffects.TryFind(x => x.Type == effectType, out GuaranteedMagicEffect guaranteedMagicEffect))
            {
                return ResolveValues(guaranteedMagicEffect, rarity);
            }

            return null;
        }

        [Obsolete("Pass the item's rarity so a per-rarity override is honoured.")]
        public static MagicItemEffectDefinition.ValueDef GetLegendaryEffectValues(string legendaryID, string effectType)
        {
            if (TryGetLegendaryInfo(legendaryID, out LegendaryInfo info) &&
                info.GuaranteedMagicEffects.TryFind(x => x.Type == effectType, out GuaranteedMagicEffect guaranteedMagicEffect))
            {
                return guaranteedMagicEffect.Values;
            }

            return null;
        }

        public static bool TryGetLegendarySetInfo(string setID, out LegendarySetInfo legendarySetInfo)
        {
            if (string.IsNullOrEmpty(setID))
            {
                legendarySetInfo = null;
                return false;
            }

            return AllSets.TryGetValue(setID, out legendarySetInfo);
        }

        [Obsolete("Sets are no longer tied to one rarity; use the two-argument overload and GetRarities.")]
        public static bool TryGetLegendarySetInfo(string setID, out LegendarySetInfo legendarySetInfo, out ItemRarity rarity)
        {
            bool found = TryGetLegendarySetInfo(setID, out legendarySetInfo);
            rarity = found ? GetRarities(legendarySetInfo).Min() : ItemRarity.Magic;
            return found;
        }

        public static string GetSetForLegendaryItem(LegendaryInfo legendary)
        {
            if (legendary != null && !string.IsNullOrEmpty(legendary.ID) &&
                ItemsToSetMap.TryGetValue(legendary.ID, out LegendarySetInfo setInfo))
            {
                return setInfo.ID;
            }

            return null;
        }

        /// <summary>
        /// How many distinct pieces make the set complete: enough to activate its largest bonus. A set can
        /// list alternative pieces for one slot (a melee and a ranged weapon), so its piece count may be
        /// more than can be worn at once.
        /// </summary>
        public static int GetFullSetCount(LegendarySetInfo setInfo)
        {
            if (setInfo == null)
            {
                return 0;
            }

            int pieceCount = setInfo.LegendaryIDs?.Where(id => !string.IsNullOrEmpty(id)).Distinct().Count() ?? 0;
            int largestBonus = setInfo.SetBonuses?.Where(x => x != null).Select(x => x.Count).DefaultIfEmpty(0).Max() ?? 0;
            if (largestBonus <= 0)
            {
                return pieceCount;
            }

            return pieceCount > 0 ? Math.Min(largestBonus, pieceCount) : largestBonus;
        }

        /// <summary>The localization token heading a set item of <paramref name="rarity"/> in its tooltip.</summary>
        public static string GetSetLabelToken(ItemRarity rarity)
        {
            return $"$mod_epicloot_{rarity.ToString().ToLowerInvariant()}setlabel";
        }
    }
}
