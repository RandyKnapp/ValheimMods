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
    private static string _ledgerSaveDirectory = Path.Combine(Paths.ConfigPath, "EpicLoot","BountySaves");
    private static string _ledgerSaveFile = Path.Combine(_ledgerSaveDirectory, $"{LedgerIdentifier}.{ZNet.m_world.m_uid}.dat");
    
    private void Awake()
    {
        Directory.CreateDirectory(_ledgerSaveDirectory);
        _instance = this;
    }
    
    private void Start()
    {
        LoadBounties();
        InvokeRepeating(nameof(SaveBounties), 0f, 60f);
    }

    private void SaveBounties()
    {
        if (!Common.Utils.IsServer() || ZoneSystem.instance == null || _bountyLedger == null)
        {
            return;
        }

        SaveTempLedger();

        var bf = new BinaryFormatter();
        var fs = File.Create(_ledgerSaveFile);

        bf.Serialize(fs,_tempBountyLedger);
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
            var ledgerDataFile = bf.Deserialize(fs) as BountyLedger;
            fs.Close();

            if (ledgerDataFile != null)
            {
                _bountyLedger = ledgerDataFile;
            }
        }
        else
        {
            var ledgerGlobalKey = globalKeys.Find(x => x.StartsWith(LedgerIdentifier,StringComparison.OrdinalIgnoreCase));
            var ledgerData = ledgerGlobalKey?.Substring(LedgerIdentifier.Length);

            if (string.IsNullOrEmpty(ledgerData))
            {
                _bountyLedger = new BountyLedger { WorldID = ZNet.m_world.m_uid };
            }
            else
            {
                try
                {
                    _bountyLedger = JsonConvert.DeserializeObject<BountyLedger>(ledgerData);
                }
                catch (Exception)
                {
                    Debug.LogWarning("[EpicLoot] WARNING! Could not load bounty kill ledger, kills made by other players may not have counted towards your bounties.");
                    _bountyLedger = new BountyLedger { WorldID = ZNet.m_world.m_uid };
                }
            }
        }
                
        foreach (var globalKey in globalKeys.Where(globalKey => globalKey.StartsWith(LedgerIdentifier,StringComparison.OrdinalIgnoreCase)))
        {
            ZoneSystem.instance.m_globalKeys.Remove(globalKey);
        }

    }

    public void Shutdown(bool save = true)
    {
        if (!Common.Utils.IsServer() || ZoneSystem.instance == null || BountyLedger == null)
        {
            return;
        }

        if (!save)
            return;
        
        SaveTempLedger();
        
        ZoneSystem.instance.m_globalKeys.RemoveWhere(x => x.StartsWith(LedgerIdentifier,StringComparison.OrdinalIgnoreCase));
        
        var ledgerData = JsonConvert.SerializeObject(_tempBountyLedger, Formatting.None);
        ledgerData = LedgerIdentifier + ledgerData;
        ZoneSystem.instance.SetGlobalKey(ledgerData);
    }

    private void SaveTempLedger()
    {
        _tempBountyLedger = _bountyLedger;
    }
}