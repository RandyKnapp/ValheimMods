using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace AdvancedPortals
{
    [HarmonyPatch]
    public static class ConnectPortals_Patch
    {
        public static readonly string[] _portalPrefabs = { "portal_ancient", "portal_obsidian", "portal_blackmarble" };
        public static bool IsPortalPrefabHash(int zdoPrefabHash)
        {
            return IsPortalPrefabHash(zdoPrefabHash, true);
        }

        public static bool IsPortalPrefabHash(int zdoPrefabHash, bool checkDefaultHash)
        {
            if (checkDefaultHash && (Game.instance.PortalPrefabHash.Contains(zdoPrefabHash)))
                return true;
            
            var stableHashCodes = _portalPrefabs.Select(x => x.GetStableHashCode()).ToArray();

            return stableHashCodes.Any(x => x.GetHashCode() == zdoPrefabHash);
        }

        //Step 1:
        //Need to Transpile ZDOMan.Load()
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.Load))]
        static class ConnectPortalsTranspiler
        {
            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    if (Debug.isDebugBuild)
                    {
                        Debug.Log($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand} Labels: {instruction.labels.Count}");                        
                    }
                    return instruction;
                }

                var portalPrefabHashMethod = AccessTools.PropertyGetter(typeof(Game), nameof(Game.PortalPrefabHash));
                var gameInstanceMethod = AccessTools.PropertyGetter(typeof(Game), nameof(Game.instance));
                var isPortalPrefabMethod = AccessTools.DeclaredMethod(typeof(ConnectPortals_Patch),
                    nameof(IsPortalPrefabHash), new[] { typeof(int) });

                var zLogLabel = ilGenerator.DefineLabel();
                
                for (int i = 0; i < instrs.Count; ++i)
                {
                    
                    if (i > 6 && instrs[i].opcode == OpCodes.Call && instrs[i].operand.Equals(gameInstanceMethod)
                        && instrs[i + 1].opcode == OpCodes.Callvirt &&
                        instrs[i + 1].operand.Equals(portalPrefabHashMethod) &&
                        instrs[i + 2].opcode == OpCodes.Bne_Un)
                    {

                        //Need to change zdo.GetPrefab() == Game.instance.PortalPrefabHash to IsPortalPrefabHash(zdo.GetPrefab())

                        //Call IsPortalPrefab Method
                        var callInstruction = new CodeInstruction(OpCodes.Call, isPortalPrefabMethod);
                        //Move Any Labels from the instruction position being patched to new instruction.
                        if (instrs[i].labels.Count > 0)
                            instrs[i].MoveLabelsTo(callInstruction);

                        yield return LogMessage(callInstruction);
                        counter++;
                        
                        //Need to change bne.un to a brfalse
                        var brfalseInstruction = new CodeInstruction(OpCodes.Brfalse, instrs[i + 2].operand);
                        yield return LogMessage(brfalseInstruction);
                        counter++;

                        //Advance to overwrite original instructions.
                        i += 2;
                    }
                    else if (i > 6 && instrs[i].opcode == OpCodes.Ldloc_3 && 
                             instrs[i + 1].opcode == OpCodes.Ldarg_1 && 
                             instrs[i + 2].opcode == OpCodes.Callvirt &&
                             instrs[i + 2].operand.Equals(AccessTools.Method(typeof(ZPackage), nameof(ZPackage.SetReader))))
                    {
                        //Read Ldarg2
                        var ldArgInstruction = new CodeInstruction(OpCodes.Ldarg_2);
                        //Move Any Labels from the instruction position being patched to new instruction.
                        if (instrs[i].labels.Count > 0)
                            instrs[i].MoveLabelsTo(ldArgInstruction);
                            
                        yield return LogMessage(ldArgInstruction);
                        counter++;
                        
                        //Read ldc_I4
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldc_I4, 31));
                        counter++;

                        //If ldArg2 is less than idc_I4 of 31, go to new label.
                        yield return LogMessage(new CodeInstruction(OpCodes.Blt, zLogLabel));
                        counter++;
                        
                        //Add original instruction
                        yield return LogMessage(instrs[i]);
                        counter++;

                    } else if (i > 6 && instrs[i].opcode == OpCodes.Ldstr && instrs[i].operand.Equals("Adding to Dictionary"))
                    {
                        //Add new label to ZLog instruction
                        instrs[i].labels.Add(zLogLabel);
                        yield return LogMessage(instrs[i]);
                        counter++;
                    }
                    else
                    {
                        yield return LogMessage(instrs[i]);
                        counter++;
                    }
                }
            }
        }

        //Step 2:
        //Need to Postfix ZDOMan private ZDO CreateNewZDO(ZDOID uid, Vector3 position, int prefabHashIn = 0)
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.CreateNewZDO),new[] { typeof(ZDOID), typeof(Vector3), typeof(int) })]
        static class ZDOManCreateNewZDOPatch
        {
            [UsedImplicitly]
            static void Postfix(ZDOMan __instance, ZDO __result, ZDOID uid, Vector3 position, int prefabHashIn = 0)
            {
                //If newZdo.GetPrefab() or prefabHashIn IsPortalPrefabHash() add to portalObjects
                if (IsPortalPrefabHash(prefabHashIn != 0 ? prefabHashIn : __result.GetPrefab(), false))
                {
                    if (!__instance.m_portalObjects.Contains(__result)) 
                        __instance.m_portalObjects.Add(__result);
                }
            }
        }        
        
        //Step 3:
        //Need to Prefix ZDOMan HandleDestroyedZDO(ZDOID uid)
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.HandleDestroyedZDO),new[] { typeof(ZDOID) })]
        static class ZDOManHandleDestroyedZDOPatch
        {
            [UsedImplicitly]
            static void Prefix(ZDOMan __instance, ZDOID uid)
            {
                //if IsPortalPrefabHash(zdo.GetPrefab()) true, remove from portalObjects
                var zdo = __instance.GetZDO(uid);
                if (zdo == null)
                    return;

                if (IsPortalPrefabHash(zdo.GetPrefab()))
                {
                    if (__instance.m_portalObjects.Contains(zdo))
                        __instance.m_portalObjects.Remove(zdo); 
                }
            }
        }        
    }
}
