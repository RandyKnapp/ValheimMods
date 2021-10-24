using EpicLoot.LegendarySystem;
using ExtendedItemDataFramework;
using fastJSON;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EpicLoot.PacketOptimization {
    public static class CustomSerialization {
        public static StringIntMapping MapTranslations { get { return _mapTranslations; } }
        private static StringIntMapping _mapTranslations = new StringIntMapping();

        public static StringIntMapping MapLegendaryIds { get { return _mapLegendaryIds; } }
        private static StringIntMapping _mapLegendaryIds = new StringIntMapping();

        public static StringIntMapping MapSetIds { get { return _mapSetIds; } }
        private static StringIntMapping _mapSetIds = new StringIntMapping();

        public static StringIntMapping MapMagicEffects { get { return _mapMagicEffects; } }
        private static StringIntMapping _mapMagicEffects = new StringIntMapping();

        //
        //
        //
        public delegate void LogAction(string message);

        public static LogAction LogInfo = null;
        public static LogAction LogWarning = null;
        public static LogAction LogError = null;


        #region Initializing String/Int mapping

        public static bool SerializeUsingCustom { get; private set; }

        public static void Initialize(bool useCustomSerialization) {
            SerializeUsingCustom = useCustomSerialization;
            LogInfo?.Invoke($"PacketOptimization.SerializeUsingCustom={SerializeUsingCustom}, ");

            LogInfo?.Invoke("Initializing PacketOptimizations...");

            _mapTranslations = LoadOrCreateMap("translations.map.json", CreateTranslationMap);
            _mapLegendaryIds = LoadOrCreateMap("legendaries.map.json", CreateLegendariesMap);
            _mapSetIds = LoadOrCreateMap("legendaries.set.map.json", CreateSetsMap);
            _mapMagicEffects = LoadOrCreateMap("magiceffects.map.json", CreateMagicEffectsMap);

            LogInfo?.Invoke("Done initializing PacketOptimizations string/int maps...");
        }

        private delegate StringIntMapping CreateMap();

        private static StringIntMapping LoadOrCreateMap(string fileName, CreateMap fallback) {
            StringIntMapping map = new StringIntMapping();
            var mapJsonText = "";
            try {
                mapJsonText = EpicLoot.LoadJsonText(fileName);
            } catch(Exception ex) {
                // fallback for when running this externally without initializing EpicLoot
                mapJsonText = File.ReadAllText(fileName);
            }

            try {
                map.Deserialize(mapJsonText);
            } catch (Exception ex) {
                LogWarning?.Invoke($"\"{fileName}\" not found... attempting to generate it");
                map = fallback();
            }

            if (map == null) {
                LogError?.Invoke($"Could not load/generate \"{fileName}\"");
            }

            return map;
        }

        private static StringIntMapping CreateTranslationMap() {
            StringIntMapping map = new StringIntMapping();

            var translationsJsonText = EpicLoot.LoadJsonText("translations.json");
            var translations = (IDictionary<string, object>)JSON.Parse(translationsJsonText);

            List<string> stringKeys = new List<string>();
            foreach (var t in translations) {
                stringKeys.Add(t.Key);
            }
            map.LoadFromList(stringKeys);

            string mapText = map.Serialize();
            string fileName = EpicLoot.GenerateAssetPathAtAssembly("translations.map.json");
            File.WriteAllText(fileName, mapText);

            return map;
        }

        private static StringIntMapping CreateLegendariesMap() {
            StringIntMapping map = new StringIntMapping();

            List<string> stringKeys = new List<string>();
            foreach (var t in UniqueLegendaryHelper.LegendaryInfo) {
                stringKeys.Add(t.Key);
            }
            map.LoadFromList(stringKeys);

            string mapText = map.Serialize();
            string fileName = EpicLoot.GenerateAssetPathAtAssembly("legendaries.map.json");
            File.WriteAllText(fileName, mapText);

            return map;
        }

        private static StringIntMapping CreateSetsMap() {
            StringIntMapping map = new StringIntMapping();

            List<string> stringKeys = new List<string>();
            foreach (var t in UniqueLegendaryHelper.LegendarySets) {
                stringKeys.Add(t.Key);
            }
            map.LoadFromList(stringKeys);

            string mapText = map.Serialize();
            string fileName = EpicLoot.GenerateAssetPathAtAssembly("legendaries.set.map.json");
            File.WriteAllText(fileName, mapText);

            return map;
        }

        private static StringIntMapping CreateMagicEffectsMap() {
            StringIntMapping map = new StringIntMapping();

            List<string> stringKeys = new List<string>();
            foreach (var t in MagicItemEffectDefinitions.AllDefinitions) {
                stringKeys.Add(t.Key);
            }
            map.LoadFromList(stringKeys);

            string mapText = map.Serialize();
            string fileName = EpicLoot.GenerateAssetPathAtAssembly("magiceffects.map.json");
            File.WriteAllText(fileName, mapText);

            return map;
        }

        #endregion

        #region Data conversion...

        public static bool EnableAutoConvert { get; private set; } = false;

        public static void InitializeConversion(bool enabled) {
            EnableAutoConvert = enabled;

            LogInfo?.Invoke($"PacketOptimization.EnableAutoConvert={EnableAutoConvert}, ");

            if (EnableAutoConvert) {
                ExtendedItemData.LoadExtendedItemData += ExtendedItemData_LoadExtendedItemData;
            } else {
                ExtendedItemData.LoadExtendedItemData -= ExtendedItemData_LoadExtendedItemData;
            }
        }


        private static void ConvertItemData(ExtendedItemData itemData) {
            bool change = false;

            string crafterNameData = itemData.m_crafterName;
            string[] components = crafterNameData.Split(new []{ ExtendedItemData.StartDelimiter }, StringSplitOptions.RemoveEmptyEntries);
            string newCrafterNameData = "";
            foreach (string component in components) {
                string[] parts = component.Split(new[] { ExtendedItemData.EndDelimiter }, StringSplitOptions.None);
                if (parts[0].StartsWith("EpicLoot")) {
                    bool isJson = parts[1].StartsWith("{");
                    if (SerializeUsingCustom && isJson) {
                        LogInfo?.Invoke("Converting magic item from JSON to CUSTOM...");
                        LogInfo?.Invoke(parts[1]);
                        parts[1] = ConvertJsonToCustom(parts[1]);
                        LogInfo?.Invoke(parts[1]);
                        change = true;
                    } else if (!SerializeUsingCustom && !isJson) {
                        LogInfo?.Invoke("Converting magic item from CUSTOM to JSON...");
                        LogInfo?.Invoke(parts[1]);
                        parts[1] = ConvertCustomToJson(parts[1]);
                        LogInfo?.Invoke(parts[1]);
                        change = true;
                    }
                }
                newCrafterNameData += ExtendedItemData.StartDelimiter + parts[0] + ExtendedItemData.EndDelimiter + parts[1];
            }

            if (change) {
                itemData.Save();
            }
        }
        private static void ExtendedItemData_LoadExtendedItemData(ExtendedItemData itemData) {
            foreach (var component in itemData.Components) {
                if (component.GetType() == typeof(MagicItemComponent)) {
                    //item has a MagicItemComponent, start processing the string...
                    ConvertItemData(itemData);
                    break;
                }
            }
        }

        public static string ConvertJsonToCustom(string json) {
            MagicItem item = JSON.ToObject<MagicItem>(json, new JSONParameters { UseExtensions = false });
            return SerializeMagicItem(item);
        }

        public static string ConvertCustomToJson(string custom) {
            MagicItem item = DeserializeMagicItem(custom);
            return JSON.ToJSON(item, new JSONParameters { UseExtensions = false });
        }



        #endregion

        #region Serialization / Deserialization

        //
        // Using these instead of changing the ItemRarity enum to int because that
        // breaks compatibility with existing save files... don't want that :(
        //
        public static int ConvertRarityToInt(ItemRarity rarity) {
            switch (rarity) {
                case ItemRarity.Magic: return 1;
                case ItemRarity.Rare: return 2;
                case ItemRarity.Epic: return 3;
                case ItemRarity.Legendary: return 4;
            }
            LogError?.Invoke("Invalid Rarity Value, this should never happen...");
            return 0;
        }        public static ItemRarity ConvertIntToRarity(int rarity) {
            switch (rarity) {
                case 1: return ItemRarity.Magic;
                case 2: return ItemRarity.Rare;
                case 3: return ItemRarity.Epic;
                case 4: return ItemRarity.Legendary;
            }
            LogError?.Invoke("Invalid Rarity Value, this should never happen...");
            return ItemRarity.Magic;
        }


        //
        // Methods for converting DisplayName text based keys to number based keys 
        // ex: "$something_key_word Object Of $something_else" becomes "$1234 Object Of $5678"
        //

        static Regex _DisplayNameRex = new Regex(@"(?<!\w)[$]\w+");
        public static string ConvertDisplayName_TextKeyToNumberKey(string input) {
            if (!input.StartsWith("$")) {
                return input;
            }

            return String.Format(CultureInfo.InvariantCulture,
                            "${0}",
                            MapTranslations.GetIntKey(input.Substring(1)));
        }
        public static string ConvertDisplayName_NumberKeyToTextKey(string input) {
            if (!input.StartsWith("$")) {
                return input;
            }

            if (!int.TryParse(input.Substring(1), out int inputInt)) {
                return input;
            }

            return "$" + MapTranslations.GetStringKey(inputInt);
        }

        public static string SerializeMagicItem(MagicItem item) {
            try {
                StringBuilder sb = new StringBuilder();

                int rarityInt = ConvertRarityToInt(item.Rarity);
                string newDisplayName = item.DisplayName == null ? null : _DisplayNameRex.Replace(item.DisplayName, match => ConvertDisplayName_TextKeyToNumberKey(match.Value));
                int legendaryIdInt = MapLegendaryIds.GetIntKey(item.LegendaryID);
                int setIdInt = MapSetIds.GetIntKey(item.SetID);

                if (legendaryIdInt == StringIntMapping.ERROR) {
                    LogError?.Invoke($"Could not correctly map {item.LegendaryID} to int...");
                }
                if (setIdInt == StringIntMapping.ERROR) {
                    LogError?.Invoke($"Could not correctly map {item.SetID} to int...");
                }

                // append fixed parameters
                sb.Append(String.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2},{3},{4},{5},{6},{7},",
                    item.Version,
                    rarityInt,
                    newDisplayName,
                    legendaryIdInt,
                    setIdInt,
                    item.TypeNameOverride,
                    item.AugmentedEffectIndex,
                    item.AugmentedEffectIndices.Count));

                // append augment indices
                for (int i = 0; i < item.AugmentedEffectIndices.Count; i++) {
                    sb.Append(String.Format(CultureInfo.InvariantCulture,
                        "{0},",
                        item.AugmentedEffectIndices[i]));
                }

                // append effect count
                sb.Append(String.Format(CultureInfo.InvariantCulture,
                            "{0},",
                            item.Effects.Count));

                // append effects
                for (int i = 0; i < item.Effects.Count; i++) {
                    MagicItemEffect effect = item.Effects[i];
                    int effectTypeInt = MapMagicEffects.GetIntKey(effect.EffectType);
                    if (effectTypeInt == StringIntMapping.ERROR) {
                        LogError?.Invoke($"Could not correctly map {effect.EffectType} to int...");
                    }
                    sb.Append(String.Format(CultureInfo.InvariantCulture,
                        "{0},{1},{2},",
                        effect.Version,
                        effectTypeInt,
                        effect.EffectValue));
                }

                return sb.ToString();
            } catch (Exception ex) {
                LogError?.Invoke("Could not serialize magic item...");
                LogError?.Invoke(ex.ToString());
                return "";
            }
        }

        public static MagicItem DeserializeMagicItem(string text) {
            try {
                MagicItem item = new MagicItem();

                string[] split = text.Split(',');

                int splitIndex = 0;

                item.Version = int.Parse(split[splitIndex++]);

                int rarityInt = int.Parse(split[splitIndex++]);
                string displayName = split[splitIndex++];
                int legendaryIdInt = int.Parse(split[splitIndex++]);
                int setIdInt = int.Parse(split[splitIndex++]);

                string newDisplayName = displayName == "" ? null : _DisplayNameRex.Replace(displayName, match => ConvertDisplayName_NumberKeyToTextKey(match.Value));

                item.Rarity = ConvertIntToRarity(rarityInt);
                item.DisplayName = newDisplayName;
                item.LegendaryID = MapLegendaryIds.GetStringKey(legendaryIdInt);
                item.SetID = MapSetIds.GetStringKey(setIdInt);

                string typeNameOverride = split[splitIndex++];
                if (typeNameOverride == "") {
                    typeNameOverride = null;
                }
                item.TypeNameOverride = typeNameOverride;

                item.AugmentedEffectIndex = int.Parse(split[splitIndex++]);
                int augmentIndexCount = int.Parse(split[splitIndex++]);
                for (int i = 0; i < augmentIndexCount; i++) {
                    int augmentIndex = int.Parse(split[splitIndex++]);
                    item.AugmentedEffectIndices.Add(augmentIndex);
                }

                int effectCount = int.Parse(split[splitIndex++]);
                for (int i = 0; i < effectCount; i++) {
                    int effectVersion = int.Parse(split[splitIndex++]);
                    int effectType = int.Parse(split[splitIndex++]);
                    float effectValue = float.Parse(split[splitIndex++]);

                    if (effectType < 0) {
                        LogError?.Invoke($"Could not deserialize effect... {effectType}");
                    } else {
                        item.Effects.Add(new MagicItemEffect() {
                            Version = effectVersion,
                            EffectType = MapMagicEffects.GetStringKey(effectType),
                            EffectValue = effectValue
                        });
                    }
                }

                return item;
            } catch (Exception ex) {
                LogError?.Invoke("Could not deserialize magic item...");
                LogError?.Invoke(ex.ToString());
                return new MagicItem();
            }
        }    
        
        private static void TestSerializeDeserialize_LogError(MagicItem item1, MagicItem item2, string serialized, string propertyName) {
            string oldValue = typeof(MagicItem).GetProperty(propertyName).GetValue(item1).ToString();
            string newValue = typeof(MagicItem).GetProperty(propertyName).GetValue(item1).ToString();

            LogError?.Invoke($"Serialization test value mismatch {propertyName}, {oldValue}, {newValue},");
            LogError?.Invoke($"JSON Data: {JSON.ToJSON(item1, new JSONParameters { UseExtensions = false })}");
            LogError?.Invoke($"CUSTOM Data: {serialized}");
        }

        private static void TestSerializeDeserialize_LogError(MagicItem item1, string serialized, string propertyName, string oldValue, string newValue) {
            LogError?.Invoke($"Serialization test value mismatch {propertyName}, {oldValue}, {newValue},");
            LogError?.Invoke($"JSON Data: {JSON.ToJSON(item1, new JSONParameters { UseExtensions = false })}");
            LogError?.Invoke($"CUSTOM Data: {serialized}");
        }

        public static bool TestSerializeDeserialize(MagicItem item) {
            string s = SerializeMagicItem(item);
            MagicItem item2 = DeserializeMagicItem(s);

            if (item.Version != item2.Version) {
                //LogError?.Invoke($"Test value mismatch Version: {item.Version}, {item2.Version}");
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.Version));
                return false;
            }
            if (item.Rarity != item2.Rarity) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.Rarity));
                //LogError?.Invoke($"Test value mismatch Rarity: {item.Rarity}, {item2.Rarity}");
                return false;
            }
            if (item.DisplayName != item2.DisplayName) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.DisplayName));
                //LogError?.Invoke($"Test value mismatch DisplayName: {item.DisplayName}, {item2.DisplayName}");
                return false;
            }
            if (item.LegendaryID != item2.LegendaryID) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.LegendaryID));
                //LogError?.Invoke($"Test value mismatch LegendaryID: {item.LegendaryID}, {item2.LegendaryID}");
                return false;
            }
            if (item.SetID != item2.SetID) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.SetID));
                //LogError?.Invoke($"Test value mismatch SetID: {item.SetID}, {item2.SetID}");
                return false;
            }
            if (item.TypeNameOverride != item2.TypeNameOverride) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.TypeNameOverride));
                //LogError?.Invoke($"Test value mismatch TypeNameOverride: {item.TypeNameOverride}, {item2.TypeNameOverride}");
                return false;
            }
            if (item.AugmentedEffectIndex != item2.AugmentedEffectIndex) {
                TestSerializeDeserialize_LogError(item, item2, s, nameof(item.AugmentedEffectIndex));
                //LogError?.Invoke($"Test value mismatch AugmentedEffectIndex: {item.AugmentedEffectIndex}, {item2.AugmentedEffectIndex}");
                return false;
            }
            if (item.AugmentedEffectIndices.Count != item2.AugmentedEffectIndices.Count) {
                TestSerializeDeserialize_LogError(item, s, "AugmentedEffectIndices.Count", item.AugmentedEffectIndices.Count.ToString(), item2.AugmentedEffectIndices.Count.ToString());
                //LogError?.Invoke($"Test value mismatch AugmentedEffectIndices.Count: {item.AugmentedEffectIndices.Count}, {item2.AugmentedEffectIndices.Count}");
                return false;
            }
            for (int i = 0; i < item.AugmentedEffectIndices.Count; i++) {
                if (item.AugmentedEffectIndices[i] != item2.AugmentedEffectIndices[i]) {
                    TestSerializeDeserialize_LogError(item, s, $"AugmentedEffectIndices[{i}]", item.AugmentedEffectIndices[i].ToString(), item2.AugmentedEffectIndices[i].ToString());
                    //LogError?.Invoke($"Test value mismatch AugmentedEffectIndices[{i}]: {item.AugmentedEffectIndices[i]}, {item2.AugmentedEffectIndices[i]}");
                    return false;
                }
            }
            if (item.Effects.Count != item2.Effects.Count) {
                TestSerializeDeserialize_LogError(item, s, "Effects.Count", item.Effects.Count.ToString(), item2.Effects.Count.ToString());
                //LogError?.Invoke($"Test value mismatch Effects.Count: {item.Effects.Count}, {item2.Effects.Count}");
                return false;
            }
            for (int i = 0; i < item.Effects.Count; i++) {
                if (item.Effects[i].Version != item2.Effects[i].Version) {
                    TestSerializeDeserialize_LogError(item, s, $"Effects[{i}].Version", item.Effects[i].Version.ToString(), item2.Effects[i].Version.ToString());
                    //LogError?.Invoke($"Test value mismatch Effects[{i}].Version: {item.Effects[i].Version}, {item2.Effects[i].Version}");
                    return false;
                }
                if (item.Effects[i].EffectType != item2.Effects[i].EffectType) {
                    TestSerializeDeserialize_LogError(item, s, $"Effects[{i}].EffectType", item.Effects[i].EffectType.ToString(), item2.Effects[i].EffectType.ToString());
                    //LogError?.Invoke($"Test value mismatch Effects[{i}].EffectType: {item.Effects[i].EffectType}, {item2.Effects[i].EffectType}");
                    return false;
                }
                if (item.Effects[i].EffectValue != item2.Effects[i].EffectValue) {
                    TestSerializeDeserialize_LogError(item, s, $"Effects[{i}].EffectValue", item.Effects[i].EffectValue.ToString(), item2.Effects[i].EffectValue.ToString());
                    //LogError?.Invoke($"Test value mismatch Effects[{i}].EffectValue: {item.Effects[i].EffectValue}, {item2.Effects[i].EffectValue}");
                    return false;
                }
            }

            LogInfo?.Invoke("Serialization test OK!");
            return true;
        }

        #endregion
    }
}
