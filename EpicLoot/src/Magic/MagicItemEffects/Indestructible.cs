using HarmonyLib;
using JetBrains.Annotations;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EpicLoot.MagicItemEffects
{
    // Indestructible is not applied through a per-effect patch. Instead the item gets its own copy of
    // SharedData with m_useDurability cleared, which switches off every vanilla drain site (Attack,
    // Humanoid armor/block, Player tool use) and every durability UI read at once.
    //
    // m_shared is runtime-only and never serialized, so the flag has to be re-derived after every
    // magic-data write and after every ItemData reconstruction. Sync() is the single entry point for
    // that; it is called from MagicItemComponent.SetMagicItem/Load, which every write path funnels
    // through, plus the Inventory.AddItem prefix below for instances whose component has not been
    // lazily constructed yet.
    public static class Indestructible
    {
        private static readonly MethodInfo memberwiseCloner = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");
        private static ItemDrop.ItemData.SharedData Clone(this ItemDrop.ItemData.SharedData sharedData) => (ItemDrop.ItemData.SharedData)memberwiseCloner.Invoke(sharedData, new object[]{});

        // The SharedData an item had before we swapped in our private copy. Restoring this exact
        // instance keeps the revert cheap and exact -- no prefab lookup, and no chance of writing
        // m_useDurability into the ObjectDB prefab's shared instance.
        private static readonly ConditionalWeakTable<ItemDrop.ItemData, ItemDrop.ItemData.SharedData> originalShared = new();

        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool))]
        public static class Indestructible_Inventory_AddItem_Patch
        {
            [UsedImplicitly]
            public static void Prefix(Inventory __instance, ref ItemDrop.ItemData item)
            {
                Sync(item);
            }
        }

        // The one drain site that ignores m_useDurability: Player.DamageArmorDurability wears a random worn
        // armor piece regardless, so an indestructible piece kept losing durability, unseen, and showed as
        // broken once the effect came off. Vanilla's pick is kept, so the other pieces wear no faster; a
        // piece that does not use durability just gets its durability back afterwards.
        //
        // A snapshot around the call rather than a transpiler on its durability write: other mods rewrite
        // that write (Valheim Admin Tool) or wear armor from their own skipping prefix (SeneaL UI's cape
        // slot), and a transpiler that missed its pattern used to abort all of Epic Loot's patching. First,
        // so the snapshot is taken before any prefix that wears armor itself. HarmonyX runs every prefix
        // and postfix even when one of them skips the original.
        [HarmonyPatch(typeof(Player), nameof(Player.DamageArmorDurability))]
        public static class Indestructible_Player_DamageArmorDurability_Patch
        {
            [UsedImplicitly]
            [HarmonyPriority(Priority.First)]
            public static void Prefix(Player __instance, out ArmorDurabilitySnapshot __state)
            {
                __state = new ArmorDurabilitySnapshot(__instance);
            }

            [UsedImplicitly]
            public static void Postfix(ArmorDurabilitySnapshot __state)
            {
                __state.Restore();
            }
        }

        /// <summary>
        /// The durability of each vanilla armor slot whose item does not use durability, taken before a hit
        /// wears armor. A struct, so a hit allocates nothing; with no such piece worn it holds nothing.
        /// </summary>
        public readonly struct ArmorDurabilitySnapshot
        {
            private readonly ItemDrop.ItemData chest, legs, helmet, shoulder;
            private readonly float chestDurability, legsDurability, helmetDurability, shoulderDurability;

            public ArmorDurabilitySnapshot(Humanoid humanoid)
            {
                chest = Keep(humanoid.m_chestItem, out chestDurability);
                legs = Keep(humanoid.m_legItem, out legsDurability);
                helmet = Keep(humanoid.m_helmetItem, out helmetDurability);
                shoulder = Keep(humanoid.m_shoulderItem, out shoulderDurability);
            }

            public void Restore()
            {
                Restore(chest, chestDurability);
                Restore(legs, legsDurability);
                Restore(helmet, helmetDurability);
                Restore(shoulder, shoulderDurability);
            }

            private static ItemDrop.ItemData Keep(ItemDrop.ItemData item, out float durability)
            {
                if (item?.m_shared == null || item.m_shared.m_useDurability)
                {
                    durability = 0f;
                    return null;
                }

                durability = item.m_durability;
                return item;
            }

            private static void Restore(ItemDrop.ItemData item, float durability)
            {
                if (item != null)
                {
                    item.m_durability = durability;
                }
            }
        }

        /// <summary>
        /// Brings the item's durability flag in line with whether it currently has the Indestructible
        /// effect (rolled or socketed). Idempotent and bidirectional -- safe to call on any item any
        /// number of times.
        /// </summary>
        public static void Sync(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return;
            }

            // HasMagicEffect includes socketed effects, so a socketed Indestructible counts.
            if (!item.HasMagicEffect(MagicEffectType.Indestructible))
            {
                Revert(item);
                return;
            }

            if (!originalShared.TryGetValue(item, out _))
            {
                originalShared.Add(item, item.m_shared);
                item.m_shared = item.m_shared.Clone();
            }

            item.m_shared.m_useDurability = false;
        }

        /// <summary>
        /// Unconditionally restores the item's original SharedData, if we ever replaced it. Used when
        /// the magic data is being torn down and can no longer be consulted (see MagicItemComponent.Unload).
        /// </summary>
        public static void Revert(ItemDrop.ItemData item)
        {
            if (item != null && originalShared.TryGetValue(item, out ItemDrop.ItemData.SharedData original))
            {
                item.m_shared = original;
                originalShared.Remove(item);
            }
        }

        /// <summary>
        /// Whether the item used durability before Indestructible was applied. Once the effect is live
        /// m_useDurability reads false, so this is the only way to tell an indestructible sword apart
        /// from an item that never had durability at all.
        /// </summary>
        public static bool OriginallyUsesDurability(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return false;
            }

            return originalShared.TryGetValue(item, out ItemDrop.ItemData.SharedData original)
                ? original.m_useDurability
                : item.m_shared.m_useDurability;
        }
    }
}
