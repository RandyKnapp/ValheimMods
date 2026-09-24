using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.Biomes;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure;

[RequireComponent(typeof(Minimap))]
public class MinimapController : MonoBehaviour
{
    private static readonly Queue<PinJob> MinimapPinQueue = new();

    private Minimap _minimap;

    public const float AreaScale = 2.1f;

    /// <summary>
    /// The adventure circle's diameter in metres - Minimap.PinData.m_worldSize is a diameter, vanilla
    /// sets it to an event's range * 2.
    /// </summary>
    public static float AreaWorldSize => Mathf.Max(0f, AdventureDataManager.Config.TreasureMap.MinimapAreaRadius) * AreaScale;

    /// <summary>
    /// The radius of the circle drawn for a bounty or a treasure map. AdventureSpawnController places
    /// inside it, so the map and the spawn cannot disagree about where the circle is.
    /// </summary>
    public static float AreaRadius => AreaWorldSize * 0.5f;

    public static readonly Dictionary<Tuple<int, Heightmap.Biome>, AreaPinInfo> TreasureMapPins = new();
    public static readonly Dictionary<string, AreaPinInfo> BountyPins = new();
    public static bool DebugMode;

    private static AdventurePinFilter _bountyPinFilter;
    private static AdventurePinFilter _treasurePinFilter;

    private static readonly List<PanelLayout> VanillaPanelLayout = new();

    public virtual void Awake()
    {
        _minimap = GetComponent<Minimap>();

        if (!_minimap.m_icons.Exists(x => x.m_name == EpicLoot.TreasureMapPinType))
        {
            _minimap.m_icons.Add(new Minimap.SpriteData
            {
                m_name = EpicLoot.TreasureMapPinType,
                m_icon = EpicAssets.MapIconTreasureMap
            });
        }

        if (!_minimap.m_icons.Exists(x => x.m_name == EpicLoot.BountyPinType))
        {
            _minimap.m_icons.Add(new Minimap.SpriteData
            {
                m_name = EpicLoot.BountyPinType,
                m_icon = EpicAssets.MapIconBounty
            });
        }
    }

    private void Start()
    {
        EnsureVisibleIconTypeCapacity();
        SetupPinFilters();
        RefreshAdventurePinFilters();
    }

    public virtual void Update()
    {
        if (Player.m_localPlayer == null)
        {
            // Do not perform operations without access to adventure data
            return;
        }

        while (MinimapPinQueue.Any())
        {
            ProcessMinimapPinTask(MinimapPinQueue.Dequeue());
        }
    }

    private void OnDestroy()
    {
        MinimapPinQueue.Clear();
        TreasureMapPins.Clear();
        BountyPins.Clear();

        _bountyPinFilter?.Destroy();
        _treasurePinFilter?.Destroy();
        _bountyPinFilter = null;
        _treasurePinFilter = null;
        VanillaPanelLayout.Clear();
    }

    private void EnsureVisibleIconTypeCapacity()
    {
        int required = (int)EpicLoot.TreasureMapPinType + 1;

        if (_minimap.m_visibleIconTypes != null && _minimap.m_visibleIconTypes.Length >= required)
        {
            return;
        }

        bool[] resized = new bool[required];

        for (int index = 0; index < resized.Length; ++index)
        {
            resized[index] = _minimap.m_visibleIconTypes == null ||
                             index >= _minimap.m_visibleIconTypes.Length ||
                             _minimap.m_visibleIconTypes[index];
        }

        _minimap.m_visibleIconTypes = resized;
    }

    private void SetupPinFilters()
    {
        if (_minimap.m_selectedIconDeath == null || _minimap.m_selectedIconBoss == null)
        {
            EpicLoot.LogError("Could not find the minimap pin filter panel, adventure pins cannot be filtered!");
            return;
        }

        _bountyPinFilter = new AdventurePinFilter(_minimap, EpicLoot.BountyPinType, EpicAssets.MapIconBounty,
            "$mod_epicloot_merchant_bounties");
        _treasurePinFilter = new AdventurePinFilter(_minimap, EpicLoot.TreasureMapPinType, EpicAssets.MapIconTreasureMap,
            "$mod_epicloot_merchant_treasuremaps");
    }

    /// <summary>
    /// Sizes the filter panel to however many adventure filters are currently shown, so hiding them leaves the
    /// vanilla layout exactly as it was found.
    /// </summary>
    private static void LayoutPinFilterPanel()
    {
        RestorePinFilterPanel();

        int addedIcons = (_bountyPinFilter is { Active: true } ? 1 : 0) +
                         (_treasurePinFilter is { Active: true } ? 1 : 0);

        if (addedIcons > 0)
        {
            GrowPinFilterPanel(addedIcons);
        }
    }

    private static void GrowPinFilterPanel(int addedIcons)
    {
        Minimap minimap = Minimap.instance;

        if (minimap == null || minimap.m_selectedIconDeath == null || minimap.m_selectedIconBoss == null)
        {
            return;
        }

        if (minimap.m_selectedIconDeath.transform.parent is not RectTransform firstIcon ||
            minimap.m_selectedIconBoss.transform.parent is not RectTransform secondIcon ||
            firstIcon.parent is not RectTransform panel || panel.parent == null)
        {
            return;
        }

        float step = (secondIcon.anchoredPosition.y - firstIcon.anchoredPosition.y) * addedIcons;
        if (step == 0f)
        {
            return;
        }

        bool upwards = step > 0f;
        float growth = Mathf.Abs(step);
        float panelEdge = panel.localPosition.y + (upwards ? panel.rect.yMax : panel.rect.yMin);
        float panelCenterX = panel.localPosition.x + panel.rect.center.x;
        float panelWidth = panel.rect.width;

        Capture(panel);
        panel.sizeDelta += new Vector2(0f, growth);
        panel.anchoredPosition += new Vector2(0f, upwards ? growth * panel.pivot.y : -growth * (1f - panel.pivot.y));

        List<RectTransform> column = new() { panel };

        foreach (RectTransform sibling in panel.parent.Cast<Transform>().OfType<RectTransform>())
        {
            if (sibling == panel || sibling.rect.width > panelWidth * 2f)
            {
                continue;
            }

            if (Mathf.Abs(sibling.localPosition.x + sibling.rect.center.x - panelCenterX) > panelWidth * 0.5f)
            {
                continue;
            }

            float siblingEdge = sibling.localPosition.y + (upwards ? sibling.rect.yMin : sibling.rect.yMax);
            if (upwards ? siblingEdge < panelEdge : siblingEdge > panelEdge)
            {
                continue;
            }

            // Nothing names the pin icon panel or the d-pad hint, so they are picked out by sitting past the
            // filter panel in its own narrow column. Re-check this if the vanilla map layout changes.
            Capture(sibling);
            sibling.localPosition += new Vector3(0f, upwards ? growth : -growth, 0f);
            column.Add(sibling);
        }

        KeepIconColumnOnMap(minimap, column);
    }

    /// <summary>
    /// Growing the panel pushes the column the added icons' worth of height off the end of the map, so the whole
    /// column slides back the other way by as much of that as the gap at the far end can take.
    /// </summary>
    private static void KeepIconColumnOnMap(Minimap minimap, List<RectTransform> column)
    {
        if (minimap.m_mapImageLarge == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        minimap.m_mapImageLarge.rectTransform.GetWorldCorners(corners);
        float mapBottom = corners[0].y;
        float mapTop = corners[1].y;

        float columnBottom = float.MaxValue;
        float columnTop = float.MinValue;

        foreach (RectTransform rect in column)
        {
            rect.GetWorldCorners(corners);
            columnBottom = Mathf.Min(columnBottom, corners[0].y);
            columnTop = Mathf.Max(columnTop, corners[1].y);
        }

        float roomAbove = mapTop - columnTop;
        float roomBelow = columnBottom - mapBottom;
        float shift = 0f;

        if (roomBelow < 0f)
        {
            shift = Mathf.Min(-roomBelow, Mathf.Max(0f, roomAbove));
        }
        else if (roomAbove < 0f)
        {
            shift = -Mathf.Min(-roomAbove, Mathf.Max(0f, roomBelow));
        }

        if (shift == 0f)
        {
            return;
        }

        foreach (RectTransform rect in column)
        {
            rect.position += new Vector3(0f, shift, 0f);
        }
    }

    private static void Capture(RectTransform rect)
    {
        VanillaPanelLayout.Add(new PanelLayout(rect));
    }

    private static void RestorePinFilterPanel()
    {
        foreach (PanelLayout layout in VanillaPanelLayout)
        {
            layout.Restore();
        }

        VanillaPanelLayout.Clear();
    }

    /// <summary>
    /// Minimap walks m_selectedIcons in insertion order for d-pad navigation, so the adventure filters have to be
    /// rebuilt into it next to the boss and death filters they sit beside rather than appended.
    /// </summary>
    private static void RebuildSelectedIcons()
    {
        Minimap minimap = Minimap.instance;

        if (minimap == null)
        {
            return;
        }

        Dictionary<Minimap.PinType, Image> rebuilt = new();
        bool inserted = false;

        foreach (KeyValuePair<Minimap.PinType, Image> entry in minimap.m_selectedIcons)
        {
            if (IsAdventurePinType(entry.Key))
            {
                continue;
            }

            rebuilt[entry.Key] = entry.Value;

            if (entry.Key != Minimap.PinType.Boss)
            {
                continue;
            }

            AddSelectedIcon(rebuilt, _bountyPinFilter);
            AddSelectedIcon(rebuilt, _treasurePinFilter);
            inserted = true;
        }

        if (!inserted)
        {
            AddSelectedIcon(rebuilt, _bountyPinFilter);
            AddSelectedIcon(rebuilt, _treasurePinFilter);
        }

        minimap.m_selectedIcons = rebuilt;
    }

    private static void AddSelectedIcon(Dictionary<Minimap.PinType, Image> icons, AdventurePinFilter filter)
    {
        if (filter is { Active: true })
        {
            icons[filter.pinType] = filter.selected;
        }
    }

    private readonly struct PanelLayout
    {
        private readonly RectTransform _rect;
        private readonly Vector2 _sizeDelta;
        private readonly Vector3 _localPosition;

        public PanelLayout(RectTransform rect)
        {
            _rect = rect;
            _sizeDelta = rect.sizeDelta;
            _localPosition = rect.localPosition;
        }

        public void Restore()
        {
            if (_rect == null)
            {
                return;
            }

            _rect.sizeDelta = _sizeDelta;
            _rect.localPosition = _localPosition;
        }
    }

    /// <summary>
    /// When not connected to a world, the filters stay active. When connected and Adventure Mode is disabled, they are hidden.
    /// </summary>
    public static void RefreshAdventurePinFilters()
    {
        bool show = ShowAdventurePinFilters();

        _bountyPinFilter?.SetActive(show);
        _treasurePinFilter?.SetActive(show);
        LayoutPinFilterPanel();
        RebuildSelectedIcons();

        PinJob pinJob = new PinJob
        {
            Task = MinimapPinQueueTask.RefreshAll
        };

        AddPinJobToQueue(pinJob);
    }

    public static void OnPinFilterToggled(Minimap.PinType type)
    {
        if (type == EpicLoot.BountyPinType)
        {
            ToggleBounties(ShowAdventureBountyPins());
        }
        else if (type == EpicLoot.TreasureMapPinType)
        {
            ToggleTreasureMaps(ShowAdventureTreasurePins());
        }
    }

    public static bool IsAdventurePinType(Minimap.PinType type)
    {
        return type == EpicLoot.BountyPinType || type == EpicLoot.TreasureMapPinType;
    }

    private static bool ShowAdventureBountyPins()
    {
        return _bountyPinFilter == null || _bountyPinFilter.Visible;
    }

    private static bool ShowAdventureTreasurePins()
    {
        return _treasurePinFilter == null || _treasurePinFilter.Visible;
    }

    private static bool ShowAdventurePinFilters()
    {
        // TODO: add more configuration options to hide minimap buttons as needed
        // Will need to ensure places this is used maintian logic if changed.
        return EpicLoot.IsAdventureModeEnabled();
    }

    private static void ToggleBounties(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        RefreshBounties(show);
    }

    private static void RefreshBounties(bool show)
    {
        if (ShowAdventurePinFilters() && show)
        {
            AdventureSaveData adventureSaveData = Player.m_localPlayer.GetAdventureSaveData();
            if (adventureSaveData == null) return;
            List<BountyInfo> currentBounties = adventureSaveData.GetInProgressBounties();

            foreach (BountyInfo bounty in currentBounties)
            {
                string key = bounty.ID;
                if (!BountyPins.ContainsKey(key))
                {
                    AreaPinInfo pinInfo = new AreaPinInfo
                    {
                        Position = bounty.Position + bounty.MinimapCircleOffset,
                        Type = EpicLoot.BountyPinType,
                        Name = Localization.instance.Localize("$mod_epicloot_bounties_minimappin", AdventureDataManager.GetBountyName(bounty))
                    };

                    PinJob pinJob = new PinJob
                    {
                        Task = MinimapPinQueueTask.AddBountyPin,
                        DebugMode = DebugMode,
                        BountyPin = new KeyValuePair<string, AreaPinInfo>(key, pinInfo)
                    };

                    AddPinJobToQueue(pinJob);
                }
            }
        }
        else
        {
            foreach (KeyValuePair<string, AreaPinInfo> pinEntry in BountyPins)
            {
                PinJob pinJob = new PinJob()
                {
                    Task = MinimapPinQueueTask.RemoveBountyPin,
                    DebugMode = DebugMode,
                    BountyPin = new KeyValuePair<string, AreaPinInfo>(pinEntry.Key, pinEntry.Value)
                };
                AddPinJobToQueue(pinJob);
            }
        }
    }
    private static void ToggleTreasureMaps(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        RefreshTreasureMaps(show);
    }

    private static void RefreshTreasureMaps(bool show)
    {
        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (ShowAdventurePinFilters() && show)
        {
            AdventureSaveData adventureSaveData = Player.m_localPlayer.GetAdventureSaveData();
            if (adventureSaveData == null)
            {
                return;
            }

            List<TreasureMapChestInfo> unfoundTreasureChests = adventureSaveData.GetUnfoundTreasureChests();

            foreach (TreasureMapChestInfo chestInfo in unfoundTreasureChests)
            {
                Tuple<int, Heightmap.Biome> key = new Tuple<int, Heightmap.Biome>(chestInfo.Interval, chestInfo.Biome);
                if (!TreasureMapPins.ContainsKey(key))
                {
                    AreaPinInfo pinInfo = new AreaPinInfo
                    {
                        Position = chestInfo.Position + chestInfo.MinimapCircleOffset,
                        Type = EpicLoot.TreasureMapPinType,
                        Name = Localization.instance.Localize("$mod_epicloot_treasurechest_minimappin",
                            Localization.instance.Localize(BiomeDataManager.GetLocalizationToken(chestInfo.Biome)),
                            (chestInfo.Interval + 1).ToString())
                    };

                    PinJob pinJob = new PinJob
                    {
                        Task = MinimapPinQueueTask.AddTreasurePin,
                        DebugMode = DebugMode,
                        TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(key, pinInfo)
                    };

                    AddPinJobToQueue(pinJob);
                }
            }
        }
        else
        {
            foreach (KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo> pinEntry in TreasureMapPins)
            {
                PinJob pinJob = new PinJob()
                {
                    Task = MinimapPinQueueTask.RemoveTreasurePin,
                    DebugMode = DebugMode,
                    TreasurePin = new KeyValuePair<Tuple<int, Heightmap.Biome>, AreaPinInfo>(pinEntry.Key, pinEntry.Value)
                };

                AddPinJobToQueue(pinJob);
            }
        }
    }

    public static void AddPinJobToQueue(PinJob pinJob)
    {
        if (pinJob != null)
        {
            MinimapPinQueue.Enqueue(pinJob);
        }
    }

    private void ProcessMinimapPinTask(PinJob pinJob)
    {
        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                if (ShowAdventureBountyPins())
                {
                    AddPin(pinJob);
                }
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                if (ShowAdventureTreasurePins())
                {
                    AddPin(pinJob);
                }
                break;
            case MinimapPinQueueTask.RemoveTreasurePin:
                RemovePin(pinJob.TreasurePin.Value);
                TreasureMapPins.Remove(pinJob.TreasurePin.Key);
                break;
            case MinimapPinQueueTask.RemoveBountyPin:
                RemovePin(pinJob.BountyPin.Value);
                BountyPins.Remove(pinJob.BountyPin.Key);
                break;
            case MinimapPinQueueTask.RefreshAll:
                RefreshPins();
                break;
        }
    }

    private void AddPin(PinJob pinJob)
    {
        AreaPinInfo newPin = null;
        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                newPin = pinJob.BountyPin.Value;
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                newPin = pinJob.TreasurePin.Value;
                break;
        }

        if (newPin == null)
        {
            return;
        }

        //Add Area Pin
        newPin.Area = _minimap.AddPin(newPin.Position, Minimap.PinType.EventArea, string.Empty, false, false);
        newPin.Area.m_worldSize = AreaWorldSize;

        //Add Pin
        newPin.Pin = _minimap.AddPin(newPin.Position, newPin.Type, newPin.Name, false, false);

        //Add Debug Pin
        if (pinJob.DebugMode)
        {
            newPin.DebugPin = _minimap.AddPin(newPin.Position, Minimap.PinType.Icon3,
                $"{newPin.Position.x:0.0}, {newPin.Position.z:0.0}", false, false);
        }

        switch (pinJob.Task)
        {
            case MinimapPinQueueTask.AddBountyPin:
                BountyPins[pinJob.BountyPin.Key] = pinJob.BountyPin.Value;
                break;
            case MinimapPinQueueTask.AddTreasurePin:
                TreasureMapPins[pinJob.TreasurePin.Key] = pinJob.TreasurePin.Value;
                break;
        }
    }

    private void RemovePin(AreaPinInfo pinEntry)
    {
        _minimap.RemovePin(pinEntry.Pin);
        _minimap.RemovePin(pinEntry.Area);

        if (pinEntry.DebugPin != null)
        {
            _minimap.RemovePin(pinEntry.DebugPin);
        }
    }

    private void RefreshPins()
    {
        ToggleBounties(ShowAdventureBountyPins());
        ToggleTreasureMaps(ShowAdventureTreasurePins());
    }
}