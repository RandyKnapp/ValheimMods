namespace EpicLoot;

using Jotunn.Managers;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class EpicAssets
{
    public static AssetBundle AssetBundle;
    public static Dictionary<string, Object> AssetCache = new Dictionary<string, Object>();

    public static Sprite EquippedSprite;
    public static Sprite AugaEquippedSprite;
    public static Sprite GenericSetItemSprite;
    public static Sprite AugaSetItemSprite;
    public static Sprite GenericItemBgSprite;
    public static Sprite AugaItemBgSprite;
    public static GameObject[] MagicItemLootBeamPrefabs = new GameObject[5];
    public static readonly Dictionary<string, GameObject[]> CraftingMaterialPrefabs =
        new Dictionary<string, GameObject[]>();
    public static Sprite SmallButtonEnchantOverlay;
    public static Sprite DodgeBuffSprite;
    public static AudioClip[] MagicItemDropSFX = new AudioClip[5];
    public static AudioClip ItemLoopSFX;
    public static AudioClip AugmentItemSFX;
    public static GameObject MerchantPanel;
    public static Sprite MapIconTreasureMap;
    public static Sprite MapIconBounty;
    public static AudioClip AbandonBountySFX;
    public static AudioClip DoubleJumpSFX;
    public static AudioClip OffSetSFX;
    public static GameObject DebugTextPrefab;
    public static GameObject AbilityBar;
    public static GameObject WelcomMessagePrefab;

    public static SE_Stats BulwarkStatusEffect;
    public static SE_Stats BerserkerStatusEffect;
    public static SE_Stats UndyingStatusEffect;
    public static SE_Stats DodgeBuffStatusEffect;

    public static GameObject BulwarkMagicShieldVFX;
    public static GameObject BulwarkMagicShieldSFX;

    public static GameObject BerserkerVFX;
    public static GameObject BerserkerSFX;

    public static GameObject UndyingVFX;
    public static GameObject UndyingSFX;

    public static GameObject DodgeBuffSFX;

    public const string Undying_SE_Name = "UndyingStatusEffect";
    public const string Bulwark_SE_Name = "BulwarkStatusEffect";
    public const string Berserker_SE_Name = "BerserkerStatusEffect";
    public const string DodgeBuff_SE_Name = "DodgeBuffStatusEffect";

    public const string ExplosiveArrow = "EL_ExplosiveArrow";

    public const string DummyName = "EL_DummyPrefab";
    public static GameObject DummyPrefab() => PrefabManager.Instance.GetPrefab(DummyName);

    public static bool AssertAssetIntegrety()
    {
        bool allFieldsPopulated = true;
        foreach (FieldInfo field in typeof(EpicAssets).GetFields())
        {
            if (field.GetValue(field) == null)
            {
                EpicLoot.LogWarning($"Asset for field {field.Name} is null! This may cause problems.");
                allFieldsPopulated = false;
            }
        }

        return allFieldsPopulated;
    }
}
