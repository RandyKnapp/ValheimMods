using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

/// <summary>
/// Exercises the public API (<see cref="API"/>) from in-game, so the endpoints other plugins depend on
/// can be smoke-tested without building a consumer plugin.
/// </summary>
public static partial class TerminalManager
{
    private static Action<ItemDrop.ItemData, string> _apiChangeLogger;
    private static Action<ItemDrop.ItemData> _apiLootLogger;

    private static readonly List<string> ApiSubcommands =
        ["version", "query", "providers", "events", "roll"];

    private static List<string> GetApiDiagnosticsOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ApiSubcommands,
            _ => []
        };
    }

    private static void ApiDiagnostics(Terminal.ConsoleEventArgs args)
    {
        string subcommand = args.Length >= 2 ? args[1].ToLowerInvariant() : "help";

        switch (subcommand)
        {
            case "version":
                ApiPrintVersion();
                break;
            case "query":
                ApiPrintQuery();
                break;
            case "providers":
                ApiPrintProviders();
                break;
            case "events":
                ApiToggleEventLogging();
                break;
            case "roll":
                ApiRollOnHeldItem(args);
                break;
            default:
                Console.instance.Print("> magicapi version   - api version, plugin version, endpoint count");
                Console.instance.Print("> magicapi query     - run every read-only endpoint against the held item");
                Console.instance.Print("> magicapi providers - list registered inventory/equipment/sacrifice providers");
                Console.instance.Print("> magicapi events    - toggle a logging listener for change and loot events");
                Console.instance.Print("> magicapi roll <rarity> - TryMakeMagicItem on the held item");
                break;
        }
    }

    private static void ApiPrintVersion()
    {
        Console.instance.Print($"> API version: {API.GetApiVersion()}");
        Console.instance.Print($"> Plugin version: {API.GetPluginVersion()} ({API.GetPluginId()})");
        Console.instance.Print($"> Endpoints: {API.GetEndpointNames().Count}");
        Console.instance.Print($"> Rarities: {API.GetRarityCount()}");

        for (int i = 0; i < API.GetRarityCount(); i++)
        {
            Console.instance.Print($">   [{i}] {API.GetRarityDisplayNameByIndex(i)} {API.GetRarityColorByIndex(i)}");
        }
    }

    /// <returns>The item the player is holding, or null (with a console message) if there is none.</returns>
    private static ItemDrop.ItemData ApiGetHeldItem()
    {
        ItemDrop.ItemData item = Player.m_localPlayer?.GetInventory()?.GetEquippedItems()?.FirstOrDefault();
        if (item == null)
        {
            Console.instance.Print("> Equip an item first.");
        }

        return item;
    }

    private static void ApiPrintQuery()
    {
        ItemDrop.ItemData item = ApiGetHeldItem();
        if (item == null)
        {
            return;
        }

        Console.instance.Print($"> {item.m_shared.m_name}");
        Console.instance.Print($">   IsMagicItem            {API.IsMagicItem(item)}");

        // Deliberately probing the non-throwing wrapper: the underlying GetRarity throws for a plain item.
        int rarity = -1;
        bool hasRarity = API.TryGetRarity(item, ref rarity);
        Console.instance.Print($">   TryGetRarity           {hasRarity} ({(hasRarity ? rarity.ToString() : "n/a")})");

        Console.instance.Print($">   GetItemRarityColor     {API.GetItemRarityColor(item)}");
        Console.instance.Print($">   IsEpicLootItem         {API.IsEpicLootItem(item)}");
        Console.instance.Print($">   IsShardStone           {API.IsShardStone(item)}");
        Console.instance.Print($">   IsRunestone            {API.IsRunestone(item)}");
        Console.instance.Print($">   IsMagicCraftingMaterial{API.IsMagicCraftingMaterial(item)}");
        Console.instance.Print($">   IsUnidentified         {API.IsUnidentified(item)}");
        Console.instance.Print($">   CanBeMagicItem         {API.CanBeMagicItem(item)}");
        Console.instance.Print($">   Effect types known     {API.GetAllMagicEffectTypes().Count}");
        Console.instance.Print($">   EnchantCosts(Magic)    {API.GetEnchantCostsJson(item, 0)}");
        Console.instance.Print($">   SacrificeProducts      {API.GetSacrificeProductsJson(item)}");
        Console.instance.Print($">   MagicItemJson          {API.GetMagicItemJson(item) ?? "<none>"}");
    }

    private static void ApiPrintProviders()
    {
        Dictionary<string, List<string>> providers = API.GetRegisteredProviders();
        foreach (KeyValuePair<string, List<string>> family in providers)
        {
            string ids = family.Value.Count == 0 ? "<none>" : string.Join(", ", family.Value);
            Console.instance.Print($"> {family.Key}: {ids}");
        }
    }

    private static void ApiToggleEventLogging()
    {
        if (_apiChangeLogger != null)
        {
            API.RemoveMagicItemChangedListener(_apiChangeLogger);
            API.RemoveLootGeneratedListener(_apiLootLogger);
            _apiChangeLogger = null;
            _apiLootLogger = null;
            Console.instance.Print("> API event logging OFF");
            return;
        }

        _apiChangeLogger = (item, reason) =>
            EpicLoot.LogForce($"[magicapi] MagicItemChanged: {item?.m_shared?.m_name} reason={reason}");
        _apiLootLogger = item =>
            EpicLoot.LogForce($"[magicapi] LootGenerated: {item?.m_shared?.m_name}");

        API.AddMagicItemChangedListener(_apiChangeLogger);
        API.AddLootGeneratedListener(_apiLootLogger);
        Console.instance.Print("> API event logging ON (writes to the BepInEx log)");
    }

    private static void ApiRollOnHeldItem(Terminal.ConsoleEventArgs args)
    {
        ItemDrop.ItemData item = ApiGetHeldItem();
        if (item == null)
        {
            return;
        }

        int rarity = args.Length >= 3 && int.TryParse(args[2], out int parsed) ? parsed : 0;
        bool result = API.TryMakeMagicItem(item, rarity, 0f, null);
        Console.instance.Print($"> TryMakeMagicItem({item.m_shared.m_name}, rarity {rarity}) = {result}");

        if (result)
        {
            Console.instance.Print($"> {API.GetMagicItemJson(item)}");
        }
    }
}
