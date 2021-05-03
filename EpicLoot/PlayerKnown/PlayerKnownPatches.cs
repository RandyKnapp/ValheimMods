using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;

namespace EpicLoot.PlayerKnown
{
    [Harmony]
    internal static class PlayerKnownPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
        private static void Post_ZNet_OnNewConnection(ZNetPeer peer)
        {
            PlayerKnownManager.OnPeerConnect(peer);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
        private static void Post_ZNet_Disconnect(ZNetPeer peer)
        {
            PlayerKnownManager.OnPeerDisconnect(peer);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        private static void Post_Player_OnSpawned(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                PlayerKnownManager.OnPlayerSpawn();
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
        private static IEnumerable<CodeInstruction> Transpile_Player_AddKnownItem(IEnumerable<CodeInstruction> instructions)
        {
            // Insert our own callback after:
            // this.m_knownMaterial.Add(item.m_shared.m_name);
            return new CodeMatcher(instructions)
                .MatchForward(true,
                    // IL_0032: ldfld    class [System.Core] System.Collections.Generic.HashSet`1<string> Player::m_knownMaterial
                    // IL_0037: ldarg.1
                    // IL_0038: ldfld    class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
                    // IL_003D: ldfld    string ItemDrop/ItemData/SharedData::m_name
                    // IL_0042: callvirt instance bool class [System.Core] System.Collections.Generic.HashSet`1<string>::Add(!0)
                    // IL_0047: pop
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Player), nameof(Player.m_knownMaterial))),
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_shared))),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_name))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<string>), nameof(HashSet<string>.Add))),
                    new CodeMatch(OpCodes.Pop)
                )
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    Transpilers.EmitDelegate<Action<ItemDrop.ItemData>>(
                        item => PlayerKnownManager.OnPlayerAddKnownItem(item.m_shared.m_name)
                    )
                )
                .InstructionEnumeration();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Player), nameof(Player.AddKnownRecipe))]
        private static IEnumerable<CodeInstruction> Transpile_Player_AddKnownRecipe(IEnumerable<CodeInstruction> instructions)
        {
            // Insert our own callback after:
            // this.m_knownRecipes.Add(recipe.m_item.m_itemData.m_shared.m_name);
            return new CodeMatcher(instructions)
                // IL_0022: ldarg.0
                // IL_0023: ldfld    class [System.Core] System.Collections.Generic.HashSet`1<string> Player::m_knownRecipes
                // IL_0028: ldarg.1
                // IL_0029: ldfld    class ItemDrop Recipe::m_item
                // IL_002E: ldfld    class ItemDrop/ItemData ItemDrop::m_itemData
                // IL_0033: ldfld    class ItemDrop/ItemData/SharedData ItemDrop/ItemData::m_shared
                // IL_0038: ldfld    string ItemDrop/ItemData/SharedData::m_name
                // IL_003D: callvirt instance bool class [System.Core] System.Collections.Generic.HashSet`1<string>::Add(!0)
                // IL_0042: pop
                .MatchForward(true,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Player), nameof(Player.m_knownRecipes))),
                    new CodeMatch(OpCodes.Ldarg_1),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Recipe), nameof(Recipe.m_item))),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop), nameof(ItemDrop.m_itemData))),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_shared))),
                    new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_name))),
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(HashSet<string>), nameof(HashSet<string>.Add))),
                    new CodeMatch(OpCodes.Pop)
                )
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_1),
                    Transpilers.EmitDelegate<Action<Recipe>>(
                        recipe => PlayerKnownManager.OnPlayerAddKnownRecipe(recipe.m_item.m_itemData.m_shared.m_name)
                    )
                )
                .InstructionEnumeration();
        }
    }
}
