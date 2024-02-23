using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using BepInEx;
using EpicLoot.Adventure.Feature;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot.Adventure;

public class BountyManagmentSystem : MonoBehaviour
{
    public static BountyManagmentSystem Instance => _instance;

    public BountyLedger BountyLedger => _bountyLedger;
    
    private BountyLedger _bountyLedger;
    private BountyLedger _tempBountyLedger;
    private static BountyManagmentSystem _instance;
    private const string LedgerIdentifier = "randyknapp.mods.epicloot.BountyLedger";
    private static string _ledgerSaveDirectory = Path.Combine(Paths.ConfigPath, "EpicLoot", "BountySaves");
    private static string _ledgerSaveFile = Path.Combine(_ledgerSaveDirectory, $"{LedgerIdentifier}.{ZNet.m_world.m_uid}.dat");

    private void Awake()
    {
        Directory.CreateDirectory(_ledgerSaveDirectory);
        _instance = this;
    }
    
    private void Start()
    {
        LoadBounties();
    }

    private void SaveBounties()
    {
        SaveTempLedger();

        var fs = File.Create(_ledgerSaveFile);

        var data = JsonConvert.SerializeObject(_tempBountyLedger);
        using (var sr = new StreamWriter(fs))
        {
            sr.Write(data);
        }
        fs.Close();
    }

    private void LoadBounties()
    {
        if (!Common.Utils.IsServer() || ZoneSystem.instance == null)
        {
            return;
        }

        var globalKeys = ZoneSystem.instance.GetGlobalKeys();

        if (File.Exists(_ledgerSaveFile))
        {
            var bf = new BinaryFormatter();
            var fs = File.Open(_ledgerSaveFile, FileMode.Open);

            using (var sr = new StreamReader(fs))
            {
                try
                {
                    // Using new file format V0.9.28
                    var data = sr.ReadToEnd();
                    _bountyLedger = JsonConvert.DeserializeObject<BountyLedger>(data);
                }
                catch (Exception e)
                {
                    // Load from original file format V0.9.27
                    fs.Position = 0;
                    _bountyLedger = bf.Deserialize(fs) as BountyLedger;
                }
            }

            fs.Close();
        }
        else
        {
            // Upgrade existing keys
            var ledgerGlobalKey = globalKeys.Find(x => x.StartsWith(LedgerIdentifier,StringComparison.OrdinalIgnoreCase));
            var ledgerData = ledgerGlobalKey?.Substring(LedgerIdentifier.Length);
            if (!ledgerData.IsNullOrWhiteSpace())
            {
                try
                {
                    _bountyLedger = JsonConvert.DeserializeObject<BountyLedger>(ledgerData);
                }
                catch (Exception)
                {
                    Debug.LogWarning("[EpicLoot] WARNING! Could not load bounty kill ledger, kills made by other players may not have counted towards your bounties.");
                }
            }
        }

        if (_bountyLedger == null)
        {
            _bountyLedger = new BountyLedger { WorldID = ZNet.m_world.m_uid };
        }

        // Upgrade existing keys by removing from global keys
        foreach (var globalKey in globalKeys.Where(globalKey => globalKey.StartsWith(LedgerIdentifier,StringComparison.OrdinalIgnoreCase)))
        {
            ZoneSystem.instance.m_globalKeys.Remove(globalKey);
        }
    }

    public void Save()
    {
        if (!Common.Utils.IsServer() || BountyLedger == null)
        {
            return;
        }

        SaveBounties();
    }

    private void SaveTempLedger()
    {
        _tempBountyLedger = _bountyLedger;
    }
}