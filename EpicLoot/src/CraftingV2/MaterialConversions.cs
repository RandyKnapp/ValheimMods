using System;
using System.Collections.Generic;
using Common;

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

        public static void Initialize(MaterialConversionsConfig config)
        {
            Config = config;

            Conversions.Clear();
            foreach (var entry in Config.MaterialConversions)
            {
                Conversions.Add(entry.Type, entry);
            }
        }


    }
}
