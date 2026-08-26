using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class IncreaseTreeDrop : IncreaseDrop
{
    public static IncreaseTreeDrop Instance { get; private set; }

    static IncreaseTreeDrop()
    {
        Instance = new IncreaseTreeDrop()
        {
            MagicEffect = MagicEffectType.IncreaseTreeDrop,
            ZDOVar = "el-tree"
        };
    }

    // Reset ZDO variable on equipment change.
    // TargetMethods because two class-level [HarmonyPatch] attributes MERGE into one target (the
    // old form only patched UnequipItem, so the ZDO tag was never cleared on equip).
    [HarmonyPatch]
    public static class IncreaseTreeDrop_Player_EquipmentChange_Patches
    {
        public static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.EquipItem));
            yield return AccessTools.DeclaredMethod(typeof(Humanoid), nameof(Humanoid.UnequipItem));
        }
        public static void Postfix(Humanoid __instance)
        {
            if (Player.m_localPlayer != null && __instance == Player.m_localPlayer && __instance.m_nview.IsValid() && __instance.m_nview.GetZDO().GetInt(Instance.ZDOVar) != 0)
            {
                __instance.m_nview.GetZDO().Set(Instance.ZDOVar, 0);
            }
        }
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Damage))]
    public static class IncreaseTreeDrop_TreeLog_Damage_Patch
    {
        private static void Prefix(TreeLog __instance, HitData hit)
        {
            Instance.DoPrefix(hit);
        }
    }

    [HarmonyPatch(typeof(TreeLog), nameof(TreeLog.Destroy))]
    public static class IncreaseTreeDrop_TreeLog_Destroy_Patch
    {
        private static void Prefix(TreeLog __instance, HitData hitData)
        {
            if (hitData != null)
            {
                Instance.TryDropExtraItems(hitData.GetAttacker(), __instance.m_dropWhenDestroyed, __instance.transform.position);
            }
        }
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.Damage))]
    public static class IncreaseTreeDrop_TreeBase_Damage_Patch
    {
        private static void Prefix(TreeBase __instance, HitData hit)
        {
            Instance.DoPrefix(hit);
        }
    }

    [HarmonyPatch(typeof(TreeBase), nameof(TreeBase.RPC_Damage))]
    public static class IncreaseTreeDrop_TreeBase_RPC_Damage_Patch
    {
        private static void Postfix(TreeBase __instance, HitData hit)
        {
            // The old guard also required 'm_nview == null', which is never true (ZNetScene.Destroy
            // defers the Unity destroy), so the bonus never dropped here. Vanilla's destroy path
            // calls gameObject.SetActive(false) synchronously inside this very RPC, and only that
            // path does -- inactive-after-call is the precise "felled by this hit" signal.
            if (hit != null && !__instance.gameObject.activeSelf)
            {
                Instance.TryDropExtraItems(hit.GetAttacker(), __instance.m_dropWhenDestroyed, __instance.transform.position);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Damage))]
    public static class IncreaseTreeDrop_Destructible_Damage_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (__instance.GetDestructibleType() == DestructibleType.Tree)
            {
                Instance.DoPrefix(hit);
            }
        }
    }

    [HarmonyPatch(typeof(Destructible), nameof(Destructible.Destroy))]
    public static class IncreaseTreeDrop_Destructible_Destroy_Patch
    {
        private static void Prefix(Destructible __instance, HitData hit)
        {
            if (hit != null && __instance.GetDestructibleType() == DestructibleType.Tree)
            {
                var dropList = __instance.gameObject.GetComponent<DropOnDestroyed>();
                if (dropList == null)
                {
                    return;
                }

                Instance.TryDropExtraItems(hit.GetAttacker(), dropList.m_dropWhenDestroyed, __instance.transform.position);
            }
        }
    }
}
