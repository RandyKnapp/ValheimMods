using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;


#nullable enable
namespace CreatureLevelControl
{
  public static class API
  {
    private static readonly Assembly? targetAssembly;

    static API()
    {
      if (!((API.targetAssembly = API.LoadAssembly()) != (Assembly) null))
        return;
      Harmony harmony = new Harmony("org.bepinex.plugins.creaturelevelcontrol.API");
      foreach (MethodInfo original in ((IEnumerable<MethodInfo>) typeof (API).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)).Where<MethodInfo>((Func<MethodInfo, bool>) (m => m.Name != "IsLoaded" && m.Name != "LoadAssembly")))
        harmony.Patch((MethodBase) original, transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof (API), "transpiler")));
    }

    private static IEnumerable<CodeInstruction> transpiler(
      IEnumerable<CodeInstruction> _,
      MethodBase original)
    {
      System.Type[] parameters = ((IEnumerable<ParameterInfo>) original.GetParameters()).Select<ParameterInfo, System.Type>((Func<ParameterInfo, System.Type>) (p =>
      {
        System.Type type = p.ParameterType;
        if (type.Assembly == Assembly.GetExecutingAssembly())
          type = API.targetAssembly.GetType(type.FullName);
        return type;
      })).ToArray<System.Type>();
      MethodBase originalMethod = (MethodBase) API.targetAssembly.GetType("CreatureLevelControl.API").GetMethod(original.Name, parameters);
      for (int i = 0; i < parameters.Length; ++i)
        yield return new CodeInstruction(OpCodes.Ldarg, (object) i);
      yield return new CodeInstruction(OpCodes.Call, (object) originalMethod);
      yield return new CodeInstruction(OpCodes.Ret);
    }

    public static Assembly? LoadAssembly() => ((IEnumerable<Assembly>) AppDomain.CurrentDomain.GetAssemblies()).SingleOrDefault<Assembly>((Func<Assembly, bool>) (a => a.GetName().Name == "CreatureLevelControl"));

    public static bool IsLoaded() => API.LoadAssembly() != (Assembly) null;

    public static bool IsEnabled() => false;

    public static bool IsInfusionEnabled() => false;

    public static bool IsExtraEffectEnabled() => false;

    public static bool IsAffixEnabled() => false;

    public static int GetWorldLevel() => 0;

    public static float[] LevelProbabilities(
      Character? character,
      int worldLevel,
      bool includeZoneBonusLevel)
    {
      return new float[3]{ 89f, 10f, 1f };
    }

    public static int LevelRand(Character character)
    {
      if (character.IsBoss() || UnityEngine.Random.Range(0, 10) != 0)
        return 1;
      return UnityEngine.Random.Range(0, 10) != 0 ? 2 : 3;
    }

    public static bool HasAffixBoss(Character character) => false;

    public static BossAffix GetAffixBoss(Character character) => BossAffix.None;

    public static void SetAffixBoss(Character character, BossAffix affix)
    {
    }

    public static bool HasExtraEffectCreature(Character character) => false;

    public static CreatureExtraEffect GetExtraEffectCreature(Character character) => CreatureExtraEffect.None;

    public static void SetExtraEffectCreature(Character character)
    {
    }

    public static void SetExtraEffectCreature(Character character, CreatureExtraEffect effect)
    {
    }

    public static bool HasInfusionCreature(Character character) => false;

    public static CreatureInfusion GetInfusionCreature(Character character) => CreatureInfusion.None;

    public static void SetInfusionCreature(Character character)
    {
    }

    public static void SetInfusionCreature(Character character, CreatureInfusion infusion)
    {
    }

    public static Character? GetTwinBoss(Character boss) => (Character) null;

    public static bool DropItemOnDeath(ItemDrop.ItemData item) => !item.m_shared.m_questItem && !item.m_equipped;
  }
}