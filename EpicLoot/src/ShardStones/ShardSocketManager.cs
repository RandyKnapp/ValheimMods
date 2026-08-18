using System;
using System.Collections.Generic;
using EpicLoot.Config;
using EpicLoot.Crafting;
using Jotunn.Managers;
using UnityEngine;

namespace EpicLoot.ShardStones {
    // How a socketed runestone/shard is allowed to leave its socket.
    public enum SocketRemoval {
        Free,      // Can be dragged back out and reused elsewhere.
        BreakOnly, // Can only be destroyed in place to free the socket.
        Locked     // Cannot leave the socket at all.
    }

    // Core socketing logic, independent of any UI. Socketed effects are stored on the equipment's
    // MagicItem (MagicItem.Sockets) and applied through the normal effect pipeline.
    public static class ShardSocketManager {
        // Resolves the effect a socketable input yields when placed into the given equipment.
        // Returns true when the input is a valid socketable; on true, `effect` may still be null for an
        // inert shard (one with no defined effect for the equipment's item type), while `color` and
        // `rarity` describe the source shard (color is None for runestones).
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
                    return false; // malformed shard
                }
                rarity = Shards.GetShardRarity(input);

                // The shard's effect depends on the host item's type. A missing mapping is a valid,
                // inert placement (effect stays null).
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

        // Whether the given runestone/shard can be socketed into the given equipment.
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

            if (!ResolveSocketedEffect(equipment, input, out var effect, out var color, out var rarity)) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Exclusive-category (e.g. boss) shards: at most one per item, and at most one across
            // worn gear. The cross-equipped rule is only enforced when the target is currently worn;
            // an unequipped item may freely receive the shard (the equip-time guard closes the loop).
            if (!CheckExclusiveCategory(equipment, color, SocketedColors(equipMagicItem.Sockets), out reason)) {
                return false;
            }

            // A shard with no defined effect for this item type may still be socketed; it sits inert.
            if (effect == null) {
                return true;
            }

            var def = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (def == null) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Reuse the rune-roll legality rules: the effect must not violate exclusivity with the item's
            // rolled effects. Host-item gating (item type/skill/etc.) is skipped for shards
            // (checkItemTypeGating: color == None) because the shard config's per-slot grid already decides
            // which effect a given slot yields; a runestone (color == None) still obeys full host gating.
            if (!def.Requirements.CheckRequirements(equipment, equipMagicItem, out var failure, out var conflictType,
                    effect.EffectType, checklootroll: false, checkaugmentroll: false, checkruneroll: true,
                    checkItemTypeGating: color == ShardType.None)) {
                // ExclusiveSelf (and self-listed ExclusiveEffectTypes) reports the SAME effect as the
                // conflict; that is exactly "the effect is already present on the item". Genuine
                // cross-effect exclusivity reports a different effect and is still enforced.
                var sameEffectAlreadyOnItem = failure == RequirementFailure.ConflictingEffect
                    && conflictType == effect.EffectType;
                if (!(sameEffectAlreadyOnItem && AllowMatchingItemEffect(color))) {
                    reason = DescribeRequirementFailure(equipMagicItem, failure, conflictType);
                    return false;
                }
            }

            var occupants = new List<SocketOccupant>();
            foreach (var socket in equipMagicItem.Sockets) {
                if (socket != null) {
                    occupants.Add(new SocketOccupant(socket.ShardType, socket.Effect));
                }
            }
            if (!CheckDuplicateEffect(color, effect, rarity, occupants, out reason)) {
                return false;
            }

            return true;
        }

        // Whether `input` may occupy a socket in `equipment` while sharing it with `coResident` (the other
        // socketables that will remain). Mirrors CanSocket's legality rules but takes the co-resident set
        // explicitly and does no free-slot check, so it fits swap validation: dragging a socketed item out
        // onto an inventory item makes vanilla push that inventory item into the vacated socket, and the
        // pushed item must obey the same duplicate/requirement rules as a fresh drop -- measured against the
        // sockets that survive once the dragged item leaves.
        public static bool CanCoexist(ItemDrop.ItemData equipment, ItemDrop.ItemData input,
            IEnumerable<ItemDrop.ItemData> coResident, out string reason) {
            reason = null;

            if (equipment == null || !equipment.IsMagic(out var equipMagicItem)) {
                reason = "$mod_epicloot_socket_notmagic";
                return false;
            }

            if (!ResolveSocketedEffect(equipment, input, out var effect, out var color, out var rarity)) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            // Exclusive-category (e.g. boss) shards obey the same one-per-item / one-across-worn-gear
            // rule on the swap path, measured against the co-resident shards that survive the swap.
            var coResidentColors = new List<ShardType>();
            foreach (var other in coResident) {
                coResidentColors.Add(Shards.GetShardColor(other));
            }
            if (!CheckExclusiveCategory(equipment, color, coResidentColors, out reason)) {
                return false;
            }

            // An inert shard (no effect for this item type) may always sit in a socket.
            if (effect == null) {
                return true;
            }

            var def = MagicItemEffectDefinitions.Get(effect.EffectType);
            if (def == null) {
                reason = "$mod_epicloot_socket_invalidinput";
                return false;
            }

            if (!def.Requirements.CheckRequirements(equipment, equipMagicItem, out var failure, out var conflictType,
                    effect.EffectType, checklootroll: false, checkaugmentroll: false, checkruneroll: true,
                    checkItemTypeGating: color == ShardType.None)) {
                // Same same-effect bypass as CanSocket, keyed by input type (see AllowMatchingItemEffect).
                var sameEffectAlreadyOnItem = failure == RequirementFailure.ConflictingEffect
                    && conflictType == effect.EffectType;
                if (!(sameEffectAlreadyOnItem && AllowMatchingItemEffect(color))) {
                    reason = DescribeRequirementFailure(equipMagicItem, failure, conflictType);
                    return false;
                }
            }

            var occupants = new List<SocketOccupant>();
            foreach (var other in coResident) {
                if (other != null &&
                    ResolveSocketedEffect(equipment, other, out var otherEffect, out var otherColor, out _)) {
                    occupants.Add(new SocketOccupant(otherColor, otherEffect));
                }
            }
            if (!CheckDuplicateEffect(color, effect, rarity, occupants, out reason)) {
                return false;
            }

            return true;
        }

        // A socket already in play, reduced to what the duplicate rules care about: what kind of stone
        // holds it, and what it grants. Serves both a stored socket and a co-resident item mid-swap.
        private readonly struct SocketOccupant {
            public readonly ShardType Color;
            public readonly MagicItemEffect Effect;

            public SocketOccupant(ShardType color, MagicItemEffect effect) {
                Color = color;
                Effect = effect;
            }
        }

        // Whether an input granting `effect` may join sockets that already grant the same effect type.
        // Shard-on-shard of the SAME COLOR is governed by the stacking mode; every other collision --
        // rune vs rune, rune vs shard, two colors landing on one effect -- stays under
        // AllowDuplicateSocketedEffects, so nothing about those changes when stacking is off.
        private static bool CheckDuplicateEffect(ShardType inputColor, MagicItemEffect effect,
            ItemRarity inputRarity, List<SocketOccupant> occupants, out string reason) {
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

                // An effect with no rarity-scaled value (Warmth, say) is a yes/no grant: a second one
                // adds nothing whether it is decayed or not, so stacking it would only cost the player
                // a socket. Same notion of "valueless" the BreakValueless removal mode uses.
                if (MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType, inputRarity)) {
                    reason = "$mod_epicloot_socket_nostackbinary";
                    return false;
                }
            }

            return true;
        }

        // Sockets the input's effect into the equipment. Returns true on success.
        public static bool AddShard(ItemDrop.ItemData equipment, ItemDrop.ItemData input) {
            if (!CanSocket(equipment, input, out _)) {
                return false;
            }

            var equipMagicItem = equipment.GetMagicItem();
            ResolveSocketedEffect(equipment, input, out var effect, out var color, out var sourceRarity);
            var sourcePrefab = GetSourcePrefabName(input);

            equipMagicItem.Sockets.Add(new SocketedEffect(effect, sourcePrefab, sourceRarity) { ShardType = color });
            RecomputeSocketValues(equipment, equipMagicItem);
            API.WithChangeReason(API.ChangeReason.Socket, () => equipment.SaveMagicItem(equipMagicItem));
            ResetCache();
            return true;
        }

        // Rebuilds every shard socket's effect from the shard grid and applies same-color stacking
        // decay. Idempotent: each value is rebuilt from the config base rather than scaled from whatever
        // is already stored, so running it after any socket change can never compound decay -- which is
        // what lets removal promote the survivors back up. Runestone sockets are left alone; their
        // effect is fixed when the rune goes in rather than derived from the host item.
        //
        // Values are baked into the socket (the whole effect pipeline and every tooltip read them from
        // there), so this must run on every path that adds, removes or re-hosts a socket.
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

                // Same resolution ResolveSocketedEffect performs, but keyed off the stored shard
                // identity rather than a loose item: a shard with no mapping for this item type sits
                // inert, it does not keep a value authored for some other kind of gear.
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

                // Strongest first, so the player always gets the best arrangement of what they slotted
                // regardless of the order they slotted it in. Value leads rather than rarity in case a
                // config's per-rarity ramp is not monotonic; rarity then socket index break ties, which
                // keeps the result deterministic.
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

        // Socket indices grouped by shard color, covering only the sockets RecomputeSocketValues
        // resolved to a value (baseValues entry is not NaN).
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

        // How the given socketed entry is allowed to leave its socket. Derived live from config and the
        // socket's own data -- nothing is persisted, so a config change applies to every existing item
        // immediately. `sourceRarity` is the shard/runestone's own rarity, the same key the shard grid
        // is indexed by in ResolveSocketedEffect.
        public static SocketRemoval GetRemovalPolicy(ShardType color, MagicItemEffect effect, ItemRarity sourceRarity) {
            if (color == ShardType.None) {
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
                    return IsValuelessGrant(effect, sourceRarity) ? SocketRemoval.BreakOnly : SocketRemoval.Free;
                case ShardSocketMode.BreakAll:
                    return SocketRemoval.BreakOnly;
                case ShardSocketMode.Permanent:
                    return SocketRemoval.Locked;
                default:
                    return SocketRemoval.Free;
            }
        }

        // Policy for a stored socket (tooltips).
        public static SocketRemoval GetRemovalPolicy(SocketedEffect socket) {
            return socket == null
                ? SocketRemoval.Free
                : GetRemovalPolicy(socket.ShardType, socket.Effect, socket.SourceRarity);
        }

        // Policy for a socketable item sitting in the socket grid (UI). Anything that isn't a valid
        // socketable has no policy to enforce.
        public static SocketRemoval GetRemovalPolicy(ItemDrop.ItemData equipment, ItemDrop.ItemData socketed) {
            return ResolveSocketedEffect(equipment, socketed, out var effect, out var color, out var rarity)
                ? GetRemovalPolicy(color, effect, rarity)
                : SocketRemoval.Free;
        }

        // True only for an effect that is present but has no rarity-scaled value -- e.g. Warmth, whose
        // magiceffects.json entry has no ValuesPerRarity block even though the shard grid hands it a
        // per-rarity number (that number is meaningless for a binary effect). A shard that grants
        // nothing at all in a slot is deliberately NOT valueless in this sense: the player gained
        // nothing from it, so it owes no commitment and stays freely removable.
        private static bool IsValuelessGrant(MagicItemEffect effect, ItemRarity sourceRarity) {
            return effect != null &&
                MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType, sourceRarity);
        }

        // The player-facing reason a socketed item may not simply be dragged out.
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

        // Removes the socket at the given index and returns a reconstructed runestone/shard item that
        // the caller should give back to the player. Returns null if the index is invalid.
        public static ItemDrop.ItemData RemoveShard(ItemDrop.ItemData equipment, int socketIndex) {
            if (equipment == null || !equipment.IsMagic(out var equipMagicItem)) {
                return null;
            }

            if (socketIndex < 0 || socketIndex >= equipMagicItem.Sockets.Count) {
                return null;
            }

            var socketed = equipMagicItem.Sockets[socketIndex];
            equipMagicItem.Sockets.RemoveAt(socketIndex);
            // Losing a shard promotes the survivors of its color back up the stacking ranks.
            RecomputeSocketValues(equipment, equipMagicItem);
            API.WithChangeReason(API.ChangeReason.Unsocket, () => equipment.SaveMagicItem(equipMagicItem));
            ResetCache();
            return ReconstructShardItem(socketed);
        }

        // Rebuilds the original Runestone/Shard item from a stored socket. Runestones carry their fixed
        // effect back; shards are rebuilt effect-less (a shard's effect is derived from the host item
        // type, so a loose shard has no baked effect), which also covers inert shard sockets.
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

            if (socketed.ShardType != ShardType.None) {
                // Loose shards carry no baked effect (it is derived from the host when socketed), and the
                // clone above already carries the source prefab's identity and magic data -- the prefab is
                // per (color, rarity), and Clone copies m_customData. Nothing left to restore.
                return item;
            }

            // Runestone: rebuild its fixed single effect (its prefab is already rarity-specific).
            var magicItem = new MagicItem { Rarity = socketed.SourceRarity };
            if (socketed.Effect != null) {
                magicItem.Effects.Add(new MagicItemEffect(socketed.Effect.EffectType, socketed.Effect.EffectValue));
            }
            item.SaveMagicItem(magicItem);
            return item;
        }

        // The socketable input is always an EtchedRunestone or a Shard of the given rarity. Deriving
        // the prefab name from type + rarity is robust regardless of m_dropPrefab being set.
        public static string GetSourcePrefabName(ItemDrop.ItemData input) {
            if (input.IsRunestone()) {
                return $"EtchedRunestone{input.GetMagicItem().Rarity}";
            }

            // Shards are one prefab per (color, rarity), and ammoType = "{Color}|{Rarity}|ShardStone"
            // carries both -- so the prefab name is rebuilt purely from the shard's own shared data.
            var color = Shards.GetShardColor(input);
            return color != ShardType.None ? $"{color}_{Shards.GetShardRarity(input)}_ShardStone" : "";
        }

        private static void ResetCache() {
            if (Player.m_localPlayer != null) {
                EquipmentEffectCache.Reset(Player.m_localPlayer);
            }
        }

        // Maps a rune-roll requirement failure (from MagicItemEffectRequirements.CheckRequirements) to a
        // specific, player-facing socket message. For a conflict, name the offending effect already on the
        // item so the player knows what stands in the way. Unknown/uncategorized failures fall back to a
        // generic message rather than claiming a wrong reason.
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

        // Whether the configured input type may socket an effect the item already carries as a rolled
        // effect. color != None => shardstone; color == None => runestone. Each type has its own toggle.
        private static bool AllowMatchingItemEffect(ShardType color) {
            return color != ShardType.None
                ? ELConfig.AllowShardstoneDuplicateItemEffect.Value
                : ELConfig.AllowRunestoneDuplicateItemEffect.Value;
        }

        // Enforces exclusive-category rules for socketing `inputColor` into `equipment`:
        //   1. Item-local: no two shards of an exclusive category may share the same item
        //      (measured against `itemLocalColors`, the shards already occupying that item).
        //   2. Cross-equipped: only one shard of an exclusive category across worn gear -- enforced
        //      only when `equipment` is currently worn (an unequipped item is caught at equip time).
        // Non-exclusive inputs (regular shards, runestones) always pass.
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

        // The shard colors currently occupying an item's sockets (None for runestone sockets).
        private static IEnumerable<ShardType> SocketedColors(IEnumerable<SocketedEffect> sockets) {
            var colors = new List<ShardType>();
            foreach (var socket in sockets) {
                colors.Add(socket != null ? socket.ShardType : ShardType.None);
            }
            return colors;
        }

        // True when any equipped magic item other than `excluding` already holds a shard of `category`.
        public static bool IsExclusiveCategoryEquipped(Player player, ShardCategory category, ItemDrop.ItemData excluding) {
            foreach (var equipped in player.GetMagicEquipment()) {
                if (equipped == excluding || !equipped.IsMagic(out var magicItem)) {
                    continue;
                }

                if (ItemHasCategory(magicItem, category)) {
                    return true;
                }
            }
            return false;
        }

        // True when any of the item's sockets holds a shard belonging to `category`.
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
