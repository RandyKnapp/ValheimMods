using BepInEx;
using EpicLoot.Adventure.Feature;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace EpicLoot.Adventure;

public class BountyManagmentSystem : MonoBehaviour
{
    public static BountyManagmentSystem Instance => _instance;

    public BountyLedger BountyLedger => _bountyLedger;
    
    private BountyLedger _bountyLedger;
    private static BountyManagmentSystem _instance;
    private const string LedgerIdentifier = "randyknapp.mods.epicloot.BountyLedger";
    private static string _ledgerSaveDirectory = Path.Combine(Paths.ConfigPath, "EpicLoot", "BountySaves");
    // Resolved in Awake, not a static initializer, so a second world hosted in the same session
    // gets its own ledger file.
    private string _ledgerSaveFile;

    public void Awake()
    {
        Directory.CreateDirectory(_ledgerSaveDirectory);
        _ledgerSaveFile = Path.Combine(_ledgerSaveDirectory, $"{LedgerIdentifier}.{ZNet.m_world.m_uid}.dat");
        _instance = this;
    }

    public void Start()
    {
        LoadBounties();
    }

    private void SaveBounties()
    {
        // Guarded: this runs from a ZNet.SaveWorld prefix, and an IO exception (file locked by
        // AV/backup, disk full) escaping it would abort the vanilla world save.
        try
        {
            var data = JsonConvert.SerializeObject(_bountyLedger);
            using (var fs = File.Create(_ledgerSaveFile))
            using (var sr = new StreamWriter(fs))
            {
                sr.Write(data);
            }
        }
        catch (Exception e)
        {
            EpicLoot.LogErrorForce($"Could not save the bounty ledger ({_ledgerSaveFile}): {e.Message}");
        }
    }

    private void LoadBounties()
    {
        if (!Common.Utils.IsServer())
        {
            return;
        }

        if (ZoneSystem.instance == null)
        {
            // Unity gives no ordering guarantee between this component's Start and ZoneSystem's
            // Awake, so retry next frame rather than leave the ledger null for the session.
            EpicLoot.LogWarning("ZoneSystem not ready when loading the bounty ledger; retrying.");
            Invoke(nameof(LoadBounties), 0f);
            return;
        }

        var globalKeys = ZoneSystem.instance.GetGlobalKeys();

        if (File.Exists(_ledgerSaveFile))
        {
            try
            {
                var bf = new BinaryFormatter();
                using (var fs = File.Open(_ledgerSaveFile, FileMode.Open))
                using (var sr = new StreamReader(fs))
                {
                    try
                    {
                        // Using new file format V0.9.28
                        var data = sr.ReadToEnd();
                        _bountyLedger = JsonConvert.DeserializeObject<BountyLedger>(data);
                    }
                    catch
                    {
                        // Load from original file format V0.9.27
                        fs.Position = 0;
                        _bountyLedger = bf.Deserialize(fs) as BountyLedger;
                    }
                }
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Could not read the bounty ledger ({_ledgerSaveFile}): {e.Message}. A new ledger will be started.");
                _bountyLedger = null;
            }
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
}