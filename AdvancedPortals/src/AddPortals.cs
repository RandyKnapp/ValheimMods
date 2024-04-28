using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace AdvancedPortals
{
  
  [HarmonyPatch]
  public static class AddPortal
  {
    public static readonly HashSet<int> Hashes = new HashSet<int>();

    [UsedImplicitly]
    private static IEnumerable<MethodInfo> TargetMethods() => new[]
    {
      AccessTools.DeclaredMethod(typeof(Game), nameof(Game.Awake)),
    };
    
    [UsedImplicitly]
    private static void Prefix(Game __instance)
    {
      __instance.PortalPrefabHash.AddRange(Hashes);
    }
  }
}