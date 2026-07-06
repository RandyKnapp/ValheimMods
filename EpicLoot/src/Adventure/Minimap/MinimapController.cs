using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure;

[RequireComponent(typeof(Minimap))]
public class MinimapController : MonoBehaviour
{
    private static readonly Queue<PinJob> MinimapPinQueue = new();

    private Minimap _minimap;

    public const float AreaScale = 2.1f;

    public static readonly Dictionary<Tuple<int, Heightmap.Biome>, AreaPinInfo> TreasureMapPins = new();
    public static readonly Dictionary<string, AreaPinInfo> BountyPins = new();
    public static bool DebugMode;

    private static GameObject _adventureToggleContainer;
    private static AdventureToggle _adventureBountyToggle;
    private static AdventureToggle _adventureTreasureToggle;

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

        SetupToggles();
    }

    private void Start()
    {
        if (_minimap.m_visibleIconTypes.Length < (int)EpicLoot.TreasureMapPinType + 1)
        {
            _minimap.m_visibleIconTypes = new bool[(int)EpicLoot.TreasureMapPinType + 1];

            for (int index = 0; index < _minimap.m_visibleIconTypes.Length; ++index)
            {
                _minimap.m_visibleIconTypes[index] = true;
            }
        }

        RefreshAdventureToggleContainer();
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
        Destroy(_adventureToggleContainer);
    }

    private void SetupToggles()
    {
        GameObject original = Utils.FindChild(_minimap.transform, "SharedPanel").gameObject;
        
        GameObject container = new GameObject("AdventureToggleContainer");
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.SetParent(original.transform.parent);

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(250f, 42f);
        rect.anchoredPosition = new Vector2(20f, 60f);
        // TODO: add repositioning configuration to be compatible with other mods

        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.spacing = 5f;

        _adventureBountyToggle = new AdventureToggle(original, rect, "Bounty", ToggleBounties);
        _adventureBountyToggle.SetIcon(EpicAssets.MapIconBounty);
        _adventureBountyToggle.SetGamepadKey("JoyLTrigger");
        _adventureBountyToggle.SetLabel("$mod_epicloot_merchant_bounties");
        _adventureBountyToggle.toggle.isOn = true;

        _adventureTreasureToggle = new AdventureToggle(original, rect, "Treasure", ToggleTreasureMaps);
        _adventureTreasureToggle.SetIcon(EpicAssets.MapIconTreasureMap);
        _adventureTreasureToggle.SetGamepadKey("JoyRTrigger");
        _adventureTreasureToggle.SetLabel("$mod_epicloot_merchant_treasuremaps");
        _adventureTreasureToggle.toggle.isOn = true;

        _adventureToggleContainer = container;
    }

    /// <summary>
    /// When not connected to a world, the container stays active. When connected and Adventure Mode is disabled, the container is hidden.
    /// </summary>
    public static void RefreshAdventureToggleContainer()
    {
        if (_adventureToggleContainer == null)
        {
            EpicLoot.LogError("There was an issue setting adventure data for the minimap, toggle container is null!");
            return;
        }

        bool show = ShowAdventureToggleContainer();
        _adventureToggleContainer.SetActive(show);

        PinJob pinJob = new PinJob
        {
            Task = MinimapPinQueueTask.RefreshAll
        };

        AddPinJobToQueue(pinJob);
    }

    private static bool ShowAdventureBountyPins()
    {
        return _adventureBountyToggle.toggle.isOn;
    }

    private static bool ShowAdventureTreasurePins()
    {
        return _adventureTreasureToggle.toggle.isOn;
    }

    private static bool ShowAdventureToggleContainer()
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
        if (ShowAdventureToggleContainer() && show)
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

        if (ShowAdventureToggleContainer() && show)
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
                            Localization.instance.Localize($"$biome_{chestInfo.Biome.ToString().ToLowerInvariant()}"),
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
        newPin.Area.m_worldSize = AdventureDataManager.Config.TreasureMap.MinimapAreaRadius * AreaScale;

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