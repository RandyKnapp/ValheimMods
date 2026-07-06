using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;


#nullable enable
namespace CreatureLevelControl
{
  [PublicAPI]
  public static class API
  {

    public static bool IsLoaded()
    {
      return false;
    }

    public static bool IsEnabled()
    {
      return false;
    }

    public static bool IsInfusionEnabled()
    {
      return false;
    }
    public static bool IsExtraEffectEnabled()
    {
      return false;
    }
    public static bool IsAffixEnabled()
    {
      return false;
    }
    public static int GetWorldLevel()
    {
      return 0;
    }

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

    public static bool HasAffixBoss(Character character) 
    {
      return false;
    }
    public static BossAffix GetAffixBoss(Character character) => BossAffix.None;

    public static void SetAffixBoss(Character character, BossAffix affix)
    {
    }

    public static bool HasExtraEffectCreature(Character character)
    {
      return false;
    }
    public static CreatureExtraEffect GetExtraEffectCreature(Character character) => CreatureExtraEffect.None;

    public static void SetExtraEffectCreature(Character character)
    {
    }

    public static void SetExtraEffectCreature(Character character, CreatureExtraEffect effect)
    {
    }

    public static bool HasInfusionCreature(Character character)
    {
      return false;
    }
    public static CreatureInfusion GetInfusionCreature(Character character) => CreatureInfusion.None;

    public static void SetInfusionCreature(Character character)
    {
    }

    public static void SetInfusionCreature(Character character, CreatureInfusion infusion)
    {
    }

    public static Character? GetTwinBoss(Character boss) => null;

    public static bool DropItemOnDeath(ItemDrop.ItemData item) => !item.m_shared.m_questItem && !item.m_equipped;
  }
}