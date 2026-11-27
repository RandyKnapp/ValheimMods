using System;
using System.Collections.Generic;
using EpicLoot.Config;
using EpicLoot.Crafting;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.ShardStones {
    public enum SocketRemoval {
        Free,
        BreakOnly,
        Locked
    }

    public static class ShardSocketManager {
        // Null-safe because MagicItem reads it on every effect sum, and a MagicItem can be read before
        // ELConfig has bound anything (or in a harness that never binds it).
        public static bool RuneSocketingEnabled => ELConfig.AllowRuneSocketing == null || ELConfig.AllowRuneSocketing.Value;

        public static bool RuneDiminishingEnabled => ELConfig.RuneDiminishingReturns != null && ELConfig.RuneDiminishingReturns.Value;

        // Rune sockets flip between granting and inert (Allow Rune Socketing) or change strength (Rune
        // Diminishing Returns) with nothing written to the items, so the memoized totals and everything
        // derived from them on equip (abilities, the loot-effect ZDO mirror) are stale until rebuilt. Fires
        // on the server push too: Jotunn applies synced values through BoxedValue, which raises SettingChanged.
        public static void OnRuneSocketRulesChanged() {
            EquipmentEffectCache.ResetAll();

            var player = Player.m_localPlayer;
            if (player == null) {
                return;
            }

            player.GetComponent<Abilities.AbilityController>()?.UpdatePlayerAbilities();
            Multiplayer_Player_Patch.WatchMultiplayerMagicEffects_Player_Patch.UpdateRichesAndLuck(player);
        }

        // On true, `effect` may still be null for an inert shard (no effect defined for the equipment's
        // item type). `color` is None for runestones.
        public static bool ResolveSocketedEffect(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            out MagicItemEffect effect, out ShardType color, out ItemRarity rarity) {
            effect = null;
            color = ShardType.None;
            rarity = ItemRarity.Magic;

            if (equipment == null || input == null) {
                return false;
            }

            if (Shards.IsShard(input)) {
                color = Shards.GetShardColor(input);
                if (color == ShardType.None) {
                    return false;
                }
                rarity = Shards.GetShardRarity(input);

                // Keep unsupported stones resolvable for existing socket saves and removal policies.
                // New placements separately require an effect, including a value at this rarity.
                var shardEffect = Shards.GetShardEffect(equipment, color);
                if (shardEffect != null && shardEffect.ValuesPerRarity.TryGetValue(rarity, out var value)) {
                    effect = new MagicItemEffect(shardEffect.EffectType, value);
                }
                return true;
            }

            if (input.IsRunestone() && input.IsMagic(out var magicItem) && magicItem.Effects.Count == 1) {
                var source = magicItem.Effects[0];
                effect = new MagicItemEffect(source.EffectType, source.EffectValue);
                rarity = magicItem.Rarity;
                return true;
            }

            return false;
        }

        // Both insertion and swaps use this gate. Do not apply it to saved-socket resolution:
        // removing an effect mapping must not delete a stone already held in an item.
        private static bool ResolvePlacementEffect(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            out MagicItemEffect effect, out ShardType color, out ItemRarity rarity, out string reason) {
            reason = null;
            if (!ResolveSocketedEffect(equipment, input, out effect, out color, out rarity)) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            if (!GatedItemType.GatedItemTypeHelper.IsSocketable(equipment)) {
                reason = "$mod_epicloot_socket_unavailable";
                return false;
            }

            if (color != ShardType.None && effect == null) {
                reason = "$mod_epicloot_socket_shardunsupported";
                return false;
            }

            if (color == ShardType.None && !RuneSocketingEnabled) {
                reason = "$mod_epicloot_socket_runesdisabled";
                return false;
            }

            return true;
        }

        public static bool CanSocket(ItemDrop.ItemData equipment, ItemDrop.ItemData input, out string reason) {
            reason = null;

            if (equipment == null || !equipment.IsMagic(out var equipMagicItem)) {
                reason = "$mod_epicloot_socket_notmagic";
                return false;
            }

            if (!equipMagicItem.HasOpenSocket()) {
                reason = "$mod_epicloot_socket_nofreeslot";
                return false;
            }

            if (!ResolvePlacementEffect(equipment, input, out var effect, out var color, out _, out reason)) {
                return false;
            }

            // The cross-equipped half is only enforced when the target is worn; UniqueShardGuard closes
            // the loop at equip time.
            if (!CheckExclusiveCategory(equipment, color, SocketedColors(equipMagicItem.Sockets), out reason)) {
                return false;
            }

            // A rune sitting inert (rune socketing off) grants nothing, so it blocks nothing either.
            var occupants = new List<SocketOccupant>();
            foreach (var socket in equipMagicItem.Sockets) {
                if (socket != null) {
                    occupants.Add(new SocketOccupant(socket.ShardType, socket.IsActive() ? socket.Effect : null));
                }
            }

            if (!CheckEffectRequirements(equipment, equipMagicItem, effect, color, occupants, out reason)) {
                return false;
            }

            if (!CheckDuplicateEffect(color, effect, occupants, out reason)) {
                return false;
            }

            if (!CheckSocketExclusivity(effect, occupants, equipMagicItem.Rarity, out reason)) {
                return false;
            }

            return true;
        }

        // CanSocket's rules for a swap, with no free-slot check: dragging a socketed item out onto an
        // inventory item makes vanilla push that item into the vacated socket, so it is measured against
        // `coResident`, the sockets that survive once the dragged item leaves.
        public static bool CanCoexist(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            IEnumerable<ItemDrop.ItemData> coResident, out string reason) {
            reason = null;

            if (equipment == null || !equipment.IsMagic(out var equipMagicItem)) {
                reason = "$mod_epicloot_socket_notmagic";
                return false;
            }

            if (!ResolvePlacementEffect(equipment, input, out var effect, out var color, out _, out reason)) {
                return false;
            }

            var coResidentColors = new List<ShardType>();
            foreach (var other in coResident) {
                coResidentColors.Add(Shards.GetShardColor(other));
            }
            if (!CheckExclusiveCategory(equipment, color, coResidentColors, out reason)) {
                return false;
            }

            var occupants = new List<SocketOccupant>();
            foreach (var other in coResident) {
                if (other != null &&
                    ResolveSocketedEffect(equipment, other, out var otherEffect, out var otherColor, out _)) {
                    var inertRune = otherColor == ShardType.None && !RuneSocketingEnabled;
                    occupants.Add(new SocketOccupant(otherColor, inertRune ? null : otherEffect));
                }
            }

            if (!CheckEffectRequirements(equipment, equipMagicItem, effect, color, occupants, out reason)) {
                return false;
            }

            if (!CheckDuplicateEffect(color, effect, occupants, out reason)) {
                return false;
            }

            if (!CheckSocketExclusivity(effect, occupants, equipMagicItem.Rarity, out reason)) {
                return false;
            }

            return true;
        }

        // The rune-roll legality rules against the item's rolled effects, with the occupants' effects meeting
        // required effects too. Host-item gating is skipped for shards because the shard grid already decides
        // which effect a slot yields.
        private static bool CheckEffectRequirements(ItemDrop.ItemData equipment, MagicItem equipMagicItem,
            MagicItemEffect effect, ShardType color, List<SocketOccupant> occupants, out string reason) {
            reason = null;

            if (!MagicItemEffectDefinitions.TryGet(effect.EffectType, out var def)) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            var socketedEffectTypes = new List<string>();
            foreach (var occupant in occupants) {
                if (occupant.Effect != null) {
                    socketedEffectTypes.Add(occupant.Effect.EffectType);
                }
            }

            if (!def.Requirements.CheckRequirements(equipment, equipMagicItem, out var failure, out var conflictType,
                    effect.EffectType, checklootroll: false, checkaugmentroll: false, checkruneroll: true,
                    checkItemTypeGating: color == ShardType.None, socketedEffectTypes: socketedEffectTypes)) {
                // ExclusiveSelf reports the same effect as the conflict, which means "already on the item";
                // a different effect is genuine cross-effect exclusivity and is still enforced.
                var sameEffectAlreadyOnItem = failure == RequirementFailure.ConflictingEffect
                    && conflictType == effect.EffectType;
                if (!(sameEffectAlreadyOnItem && AllowMatchingItemEffect(color))) {
                    reason = DescribeRequirementFailure(equipMagicItem, failure, conflictType);
                    return false;
                }
            }

            return true;
        }

        private readonly struct SocketOccupant {
            public readonly ShardType Color;
            public readonly MagicItemEffect Effect;

            public SocketOccupant(ShardType color, MagicItemEffect effect) {
                Color = color;
                Effect = effect;
            }
        }

        // Same-color shards obey the stacking mode; every other collision (rune vs rune, rune vs shard,
        // two colors on one effect) obeys AllowDuplicateSocketedEffects.
        private static bool CheckDuplicateEffect(ShardType inputColor, MagicItemEffect effect,
            List<SocketOccupant> occupants, out string reason) {
            reason = null;

            foreach (var occupant in occupants) {
                if (occupant.Effect == null || occupant.Effect.EffectType != effect.EffectType) {
                    continue;
                }

                var sameColorShard = inputColor != ShardType.None && occupant.Color == inputColor;
                if (!sameColorShard) {
                    if (ELConfig.AllowDuplicateSocketedEffects.Value) {
                        continue;
                    }
                    reason = "$mod_epicloot_socket_duplicate";
                    return false;
                }

                if (ELConfig.ShardStackingMode.Value == ShardStackMode.Blocked) {
                    reason = "$mod_epicloot_socket_duplicate";
                    return false;
                }

                // A yes/no grant (Warmth, say): a second one adds nothing, so stacking it would only
                // cost the player a socket.
                if (MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType)) {
                    reason = "$mod_epicloot_socket_nostackbinary";
                    return false;
                }
            }

            return true;
        }

        // Exclusion between sockets; against the rolled effects it is CheckRequirements'.
        private static bool CheckSocketExclusivity(MagicItemEffect effect, List<SocketOccupant> occupants,
            ItemRarity rarity, out string reason) {
            reason = null;

            foreach (var occupant in occupants) {
                if (occupant.Effect != null &&
                    MagicItemEffectRequirements.EffectsExclude(effect.EffectType, occupant.Effect.EffectType)) {
                    var effectText = MagicItem.GetEffectText(occupant.Effect, rarity, false);
                    reason = $"$mod_epicloot_socket_conflict: {effectText}";
                    return false;
                }
            }

            return true;
        }

        // Rebuilds from the config base rather than scaling what is stored, so it never compounds decay
        // and removal promotes the survivors back up. Values are baked into the socket, so this must run
        // on every path that adds, removes or re-hosts a socket. Rune sockets are left alone; their
        // diminishing returns are applied on read (GetRuneStackMultiplier).
        public static void RecomputeSocketValues(ItemDrop.ItemData equipment, MagicItem magicItem) {
            if (equipment == null || magicItem == null || magicItem.Sockets.Count == 0) {
                return;
            }

            // Base (undecayed) value per socket; NaN marks a socket this pass does not rank -- a
            // runestone, an inert shard, or one whose color has no effect on this item type.
            var baseValues = new float[magicItem.Sockets.Count];
            for (var i = 0; i < magicItem.Sockets.Count; i++) {
                baseValues[i] = float.NaN;
                var socket = magicItem.Sockets[i];
                if (socket == null) {
                    continue;
                }

                socket.StackMultiplier = 1f;
                if (socket.ShardType == ShardType.None) {
                    continue;
                }

                // A shard with no mapping for this item type sits inert rather than keeping a value
                // authored for some other kind of gear.
                var shardEffect = Shards.GetShardEffect(equipment, socket.ShardType);
                if (shardEffect == null ||
                    !shardEffect.ValuesPerRarity.TryGetValue(socket.SourceRarity, out var baseValue)) {
                    socket.Effect = null;
                    continue;
                }

                socket.Effect = new MagicItemEffect(shardEffect.EffectType, baseValue);
                baseValues[i] = baseValue;
            }

            if (ELConfig.ShardStackingMode.Value != ShardStackMode.Diminishing) {
                return;
            }

            var decay = ELConfig.ShardStackDecayFactor.Value;
            foreach (var group in GroupSocketsByColor(magicItem, baseValues)) {
                if (group.Count < 2) {
                    continue;
                }

                // Strongest first, so slotting order does not matter. Value leads rather than rarity in
                // case a config's per-rarity ramp is not monotonic.
                group.Sort((a, b) => {
                    var byValue = baseValues[b].CompareTo(baseValues[a]);
                    if (byValue != 0) {
                        return byValue;
                    }
                    var byRarity = magicItem.Sockets[b].SourceRarity.CompareTo(magicItem.Sockets[a].SourceRarity);
                    return byRarity != 0 ? byRarity : a.CompareTo(b);
                });

                for (var rank = 1; rank < group.Count; rank++) {
                    var socket = magicItem.Sockets[group[rank]];
                    var multiplier = Mathf.Pow(decay, rank);
                    socket.StackMultiplier = multiplier;
                    // Two decimals: the raw product runs to 1.5 * 0.125 = 0.1875, which reads as noise
                    // in a tooltip and buys nothing at these magnitudes.
                    socket.Effect.EffectValue = (float)Math.Round(baseValues[group[rank]] * multiplier, 2);
                }
            }
        }

        // Rolled effects and shard sockets of the same effect rank ahead of a rune; runes rank among
        // themselves strongest first. Worked out on read rather than baked like shard decay: a rune's
        // stored effect is the rune itself (ReconstructShardItem hands it back, crafting transfer and
        // tempering copy it), and its rank depends on rolled effects that change with no socket path running.
        public static float GetRuneStackMultiplier(MagicItem magicItem, int index) {
            if (!RuneDiminishingEnabled) {
                return 1f;
            }

            var socket = magicItem.Sockets[index];
            if (socket?.Effect == null || socket.ShardType != ShardType.None) {
                return 1f;
            }

            var effectType = socket.Effect.EffectType;
            var rank = 0;
            foreach (var rolled in magicItem.Effects) {
                if (rolled.EffectType == effectType) {
                    rank++;
                }
            }

            for (var i = 0; i < magicItem.Sockets.Count; i++) {
                var other = magicItem.Sockets[i];
                if (i == index || other?.Effect == null || other.Effect.EffectType != effectType) {
                    continue;
                }

                if (other.ShardType != ShardType.None || RuneRanksAhead(other, i, socket, index)) {
                    rank++;
                }
            }

            // A yes/no grant has nothing to decay. Checked last: it is the one lookup here.
            if (rank == 0 || MagicItemEffectDefinitions.IsValuelessEffect(effectType)) {
                return 1f;
            }

            return Mathf.Pow(ELConfig.RuneStackDecayFactor.Value, rank);
        }

        // Strongest first; rarity then socket index break ties, as in RecomputeSocketValues.
        private static bool RuneRanksAhead(SocketedEffect a, int aIndex, SocketedEffect b, int bIndex) {
            if (a.Effect.EffectValue != b.Effect.EffectValue) {
                return a.Effect.EffectValue > b.Effect.EffectValue;
            }
            if (a.SourceRarity != b.SourceRarity) {
                return a.SourceRarity > b.SourceRarity;
            }
            return aIndex < bIndex;
        }

        private static List<List<int>> GroupSocketsByColor(MagicItem magicItem, float[] baseValues) {
            var groups = new Dictionary<ShardType, List<int>>();
            for (var i = 0; i < magicItem.Sockets.Count; i++) {
                if (float.IsNaN(baseValues[i])) {
                    continue;
                }

                var color = magicItem.Sockets[i].ShardType;
                if (!groups.TryGetValue(color, out var indices)) {
                    indices = new List<int>();
                    groups[color] = indices;
                }
                indices.Add(i);
            }
            return new List<List<int>>(groups.Values);
        }

        // Derived live, never persisted, so a config change applies to existing items at once.
        public static SocketRemoval GetRemovalPolicy(ShardType color, MagicItemEffect effect) {
            if (color == ShardType.None) {
                // With rune socketing off a slotted rune grants nothing, so it owes no commitment.
                if (!RuneSocketingEnabled) {
                    return SocketRemoval.Free;
                }

                switch (ELConfig.RuneSocketRemovalMode.Value) {
                    case RuneSocketMode.Break:
                        return SocketRemoval.BreakOnly;
                    case RuneSocketMode.Permanent:
                        return SocketRemoval.Locked;
                    default:
                        return SocketRemoval.Free;
                }
            }

            switch (ELConfig.ShardSocketRemovalMode.Value) {
                case ShardSocketMode.BreakValueless:
                    return IsValuelessGrant(effect) ? SocketRemoval.BreakOnly : SocketRemoval.Free;
                case ShardSocketMode.BreakAll:
                    return SocketRemoval.BreakOnly;
                case ShardSocketMode.Permanent:
                    return SocketRemoval.Locked;
                default:
                    return SocketRemoval.Free;
            }
        }

        public static SocketRemoval GetRemovalPolicy(SocketedEffect socket) {
            return socket == null
                ? SocketRemoval.Free
                : GetRemovalPolicy(socket.ShardType, socket.Effect);
        }

        public static SocketRemoval GetRemovalPolicy(ItemDrop.ItemData equipment, ItemDrop.ItemData socketed) {
            return ResolveSocketedEffect(equipment, socketed, out var effect, out var color, out var rarity)
                ? GetRemovalPolicy(color, effect)
                : SocketRemoval.Free;
        }

        // A shard that grants nothing at all in a slot is deliberately not valueless: the player gained
        // nothing from it, so it stays freely removable.
        private static bool IsValuelessGrant(MagicItemEffect effect) {
            return effect != null && MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType);
        }

        public static string DescribeRemovalPolicy(SocketRemoval policy) {
            switch (policy) {
                case SocketRemoval.BreakOnly:
                    return "$mod_epicloot_socket_mustbreak";
                case SocketRemoval.Locked:
                    return "$mod_epicloot_socket_permanent";
                default:
                    return null;
            }
        }

        public static ItemDrop.ItemData ReconstructShardItem(SocketedEffect socketed) {
            if (socketed == null || string.IsNullOrEmpty(socketed.SourcePrefab)) {
                return null;
            }

            var prefab = PrefabManager.Instance.GetPrefab(socketed.SourcePrefab);
            if (prefab == null) {
                EpicLoot.LogErrorForce($"Could not reconstruct socketed item, missing prefab '{socketed.SourcePrefab}'");
                return null;
            }

            var baseData = prefab.GetComponent<ItemDrop>();
            if (baseData == null) {
                return null;
            }

            var item = baseData.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = 1;
            // A prefab's data carries world level 0, and Inventory merges stacks only when the world level
            // matches (ItemData.IsSameType), so on a New Game+ world a stone handed back from a socket would
            // never join the player's stacks. Stamp the current level, as vanilla does on every drop it
            // creates (CharacterDrop, DropTable) and LootRoller does on loot. The socket does not record the
            // level the stone went in with, so this is the one level it can come back at.
            item.m_worldLevel = Game.m_worldLevel;

            // A loose shard has no effect of its own; the per-(color, rarity) prefab clone is the whole stone.
            if (socketed.ShardType != ShardType.None) {
                return item;
            }

            var magicItem = new MagicItem { Rarity = socketed.SourceRarity };
            if (socketed.Effect != null) {
                magicItem.Effects.Add(new MagicItemEffect(socketed.Effect.EffectType, socketed.Effect.EffectValue));
            }
            item.SaveMagicItem(magicItem);
            return item;
        }

        // Built from type and rarity rather than m_dropPrefab, which may not be set.
        public static string GetSourcePrefabName(ItemDrop.ItemData input) {
            if (input.IsRunestone()) {
                return $"EtchedRunestone{input.GetMagicItem().Rarity}";
            }

            var color = Shards.GetShardColor(input);
            return color != ShardType.None ? $"{color}_{Shards.GetShardRarity(input)}_ShardStone" : "";
        }

        private static void ResetCache() {
            if (Player.m_localPlayer != null) {
                EquipmentEffectCache.Reset(Player.m_localPlayer);
            }
        }

        private static string DescribeRequirementFailure(MagicItem magicItem, RequirementFailure failure, string conflictEffectType) {
            switch (failure) {
                case RequirementFailure.ConflictingEffect:
                    var existing = conflictEffectType != null
                        ? magicItem.Effects.Find(e => e.EffectType == conflictEffectType)
                        : null;
                    if (existing != null) {
                        var effectText = MagicItem.GetEffectText(existing, magicItem.Rarity, false);
                        return $"$mod_epicloot_socket_conflict: {effectText}";
                    }
                    return "$mod_epicloot_socket_conflict";
                case RequirementFailure.MissingRequiredEffect:
                    return "$mod_epicloot_socket_missingreq";
                case RequirementFailure.RarityNotAllowed:
                    return "$mod_epicloot_socket_raritynotallowed";
                case RequirementFailure.ItemPropertyMismatch:
                    return "$mod_epicloot_socket_itemreq";
                case RequirementFailure.ItemTypeNotAllowed:
                    return "$mod_epicloot_socket_notallowed";
                default:
                    return "$mod_epicloot_socket_generic";
            }
        }

        // Also read the other way round, for a rolled effect joining a matching socket
        // (MagicItemEffectDefinitions.GetAvailableEffects).
        internal static bool AllowMatchingItemEffect(ShardType color) {
            return color != ShardType.None
                ? ELConfig.AllowShardstoneDuplicateItemEffect.Value
                : ELConfig.AllowRunestoneDuplicateItemEffect.Value;
        }

        private static bool CheckExclusiveCategory(ItemDrop.ItemData equipment, ShardType inputColor,
            IEnumerable<ShardType> itemLocalColors, out string reason) {
            reason = null;

            if (inputColor == ShardType.None) {
                return true;
            }

            var category = Shards.GetCategory(inputColor);
            if (!Shards.IsExclusive(category)) {
                return true;
            }

            foreach (var color in itemLocalColors) {
                if (color != ShardType.None && Shards.GetCategory(color) == category) {
                    reason = $"$mod_epicloot_socket_{Shards.ExclusiveCategorySlug(category)}limit";
                    return false;
                }
            }

            var player = Player.m_localPlayer;
            if (player != null && player.IsItemEquiped(equipment) &&
                IsExclusiveCategoryEquipped(player, category, equipment)) {
                reason = $"$mod_epicloot_socket_{Shards.ExclusiveCategorySlug(category)}limit";
                return false;
            }

            return true;
        }

        private static IEnumerable<ShardType> SocketedColors(IEnumerable<SocketedEffect> sockets) {
            var colors = new List<ShardType>();
            foreach (var socket in sockets) {
                colors.Add(socket != null ? socket.ShardType : ShardType.None);
            }
            return colors;
        }

        // `displaced` names the worn items an in-flight Humanoid.EquipItem is about to unequip, so the
        // equip guard measures the loadout the equip will produce.
        public static bool IsExclusiveCategoryEquipped(Player player, ShardCategory category,
            ItemDrop.ItemData excluding, EquipDisplacement.Displaced displaced = default) {
            foreach (var equipped in player.GetMagicEquipment()) {
                if (equipped == excluding || displaced.Contains(equipped) || !equipped.IsMagic(out var magicItem)) {
                    continue;
                }

                if (ItemHasCategory(magicItem, category)) {
                    return true;
                }
            }
            return false;
        }

        public static bool ItemHasCategory(MagicItem magicItem, ShardCategory category) {
            foreach (var socket in magicItem.Sockets) {
                if (socket != null && socket.ShardType != ShardType.None &&
                    Shards.GetCategory(socket.ShardType) == category) {
                    return true;
                }
            }
            return false;
        }
    }
}
