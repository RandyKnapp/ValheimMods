using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using static EquipmentAndQuickSlots.Slots;

namespace EquipmentAndQuickSlots.src.MultiUtility {
    // Rendering the second and third utility item on the character model.
    //
    // VisEquipment has one utility slot (m_utilityItem / m_utilityItemInstances) driven by one ZDO
    // value, so the extras need their own. This mirrors vanilla's own shape exactly: the wearer
    // writes a prefab-name hash into a custom ZDO int, and every client — including everyone
    // watching a remote player — reads it back and attaches the same armor instances vanilla
    // would. Nothing here touches gameplay; MultiUtility owns that.
    //
    // Because the value travels on the wearer's ZDO, "Show extra utility items" is the wearer's
    // setting: turning it off writes empty and the items disappear for everyone.
    internal static class MultiUtilityVisuals {
        private static readonly int[] zdoKeys = {
            "EAQS_ExtraUtility_1".GetStableHashCode(),
            "EAQS_ExtraUtility_2".GetStableHashCode(),
        };

        // One entry per extra slot, laid out like VisEquipment's own per-slot state: the prefab
        // name last written, the hash currently attached, and the spawned instances.
        private class VisState {
            internal readonly string[] names = new string[zdoKeys.Length];
            internal readonly int[] attachedHashes = new int[zdoKeys.Length];
            internal readonly List<GameObject>[] instances = new List<GameObject>[zdoKeys.Length];
        }

        private static readonly ConditionalWeakTable<VisEquipment, VisState> states = new ConditionalWeakTable<VisEquipment, VisState>();

        // Write side, mirroring VisEquipment.SetUtilityItem: only the owner publishes, and an
        // unchanged name is not republished.
        private static void SetItem(VisEquipment visEq, int index, string name) {
            VisState state = states.GetOrCreateValue(visEq);
            if (state.names[index] == name)
                return;

            state.names[index] = name;

            ZDO zdo = visEq.m_nview == null ? null : visEq.m_nview.GetZDO();
            if (zdo == null || !visEq.m_nview.IsOwner())
                return;

            zdo.Set(zdoKeys[index], string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode());
        }

        // Read side, mirroring VisEquipment.SetUtilityEquipped. Returns true when the attached
        // instances changed, so the caller knows the LOD group needs rebuilding.
        private static bool SetEquipped(VisEquipment visEq, int index, int hash) {
            VisState state = states.GetOrCreateValue(visEq);
            if (state.attachedHashes[index] == hash)
                return false;

            if (state.instances[index] != null) {
                foreach (GameObject instance in state.instances[index]) {
                    if (visEq.m_lodGroup)
                        Utils.RemoveFromLodgroup(visEq.m_lodGroup, instance);

                    Object.Destroy(instance);
                }

                state.instances[index] = null;
            }

            state.attachedHashes[index] = hash;
            if (hash != 0)
                state.instances[index] = visEq.AttachArmor(hash);

            return true;
        }

        private static bool HasAnyExtra(ZDO zdo) {
            for (int i = 0; i < zdoKeys.Length; i++)
                if (zdo.GetInt(zdoKeys[i]) != 0)
                    return true;

            return false;
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.SetupVisEquipment))]
        private static class Humanoid_SetupVisEquipment_PublishExtraUtility {
            private static void Postfix(Humanoid __instance, VisEquipment visEq, bool isRagdoll) {
                if (!visEq || isRagdoll || __instance is not Player player)
                    return;

                bool show = ValConfig.ShowExtraUtilityItems?.Value ?? true;

                for (int i = 0; i < zdoKeys.Length; i++) {
                    ItemDrop.ItemData item = show ? MultiUtility.GetExtra(player, i) : null;
                    SetItem(visEq, i, item?.m_dropPrefab != null ? item.m_dropPrefab.name : "");
                }
            }
        }

        // Postfix rather than a prefix: vanilla only rebuilds the LOD group when one of its own
        // slots changed, so anything we attach here has to ask for the rebuild itself. Doing it
        // after vanilla means at worst one extra rebuild on the frame a utility item changes —
        // UpdateLodgroup is idempotent, and this is not a per-frame path.
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
        private static class VisEquipment_UpdateEquipmentVisuals_AttachExtraUtility {
            private static void Postfix(VisEquipment __instance) {
                ZDO zdo = __instance.m_nview == null ? null : __instance.m_nview.GetZDO();

                // This runs for every character in the world, the overwhelming majority of which
                // will never have an extra utility item. Don't give them per-instance state until
                // there is actually something to attach.
                if (!states.TryGetValue(__instance, out VisState state)) {
                    if (zdo == null || !HasAnyExtra(zdo))
                        return;

                    state = states.GetOrCreateValue(__instance);
                }

                bool changed = false;
                for (int i = 0; i < zdoKeys.Length; i++) {
                    // No ZDO means a ragdoll or the character-selection preview: nothing is
                    // networked, so the name we last wrote locally is the whole truth.
                    int hash = zdo != null
                        ? zdo.GetInt(zdoKeys[i])
                        : string.IsNullOrEmpty(state.names[i]) ? 0 : state.names[i].GetStableHashCode();

                    changed |= SetEquipped(__instance, i, hash);
                }

                if (changed)
                    __instance.UpdateLodgroup();
            }
        }
    }
}
