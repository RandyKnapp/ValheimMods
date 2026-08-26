using Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.CraftingV2
{
    [Serializable]
    public enum MaterialConversionType
    {
        Upgrade,
        Convert,
        Junk
    }

    [Serializable]
    public class MaterialConversionRequirement
    {
        public string Item = "";
        public int Amount = 1;
    }

    [Serializable]
    public class MaterialConversion
    {
        public string Name = "";
        public string Product = "";
        public int Amount = 1;
        public MaterialConversionType Type;
        public List<MaterialConversionRequirement> Resources = new List<MaterialConversionRequirement>();
    }

    [Serializable]
    public class MaterialConversionsConfig
    {
        public List<MaterialConversion> MaterialConversions = new List<MaterialConversion>();
    }

    public static class MaterialConversions
    {
        public static MaterialConversionsConfig Config;
        public static MultiValueDictionary<MaterialConversionType, MaterialConversion> Conversions = new MultiValueDictionary<MaterialConversionType, MaterialConversion>();
        public static event Action OnSetupMaterialConversions;

        public static void Initialize(MaterialConversionsConfig config)
        {
            if (config == null)
            {
                EpicLoot.LogWarning("MaterialConversions.Initialize called with a null config; keeping the currently loaded conversions.");
                return;
            }

            Config = config;
            OnSetupMaterialConversions?.Invoke();

            Conversions.Clear();
            foreach (var entry in Config.MaterialConversions)
            {
                Conversions.Add(entry.Type, entry);
            }
        }

        // What a dedicated server pushes to each client. The shardstone recipes are merged into Config at
        // load but ship in their own config with its own RPC, and every client re-merges them from that
        // copy, so sending them here would duplicate several hundred entries in the payload for nothing.
        public static MaterialConversionsConfig GetCFG()
        {
            return new MaterialConversionsConfig
            {
                MaterialConversions = Config.MaterialConversions
                    .Where(x => !ShardStones.ShardStoneConversions.IsShardStoneRecipe(x))
                    .ToList()
            };
        }
    }
}
