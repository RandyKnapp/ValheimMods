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

    // Reset ZDO variable on equipment change
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
    public static class IncreaseTreeDrop_Player_EquipmentChange_Patches
    {
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
            if (hit != null && __instance.m_nview == null && !__instance.gameObject.activeSelf)
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
