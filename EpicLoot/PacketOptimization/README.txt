    This attempts to improve EpicLoot's lag problem by replacing the JSON serialization 
with CUSTOM serialization.
    The *CUSTOM* serialization achieves this by remapping unique string keys to unique int 
keys and just generally having way less overhead than JSON.


--- Magic Item Serialization Structure ---
    The "MagicItem" object is serialized as a series of comma separated values 
in the following order:
+ "Version" - Integer value, unchanged
+ "Rarity" - Hardcoded enum value to integer value
    * The enum was not overriden to be integer based since this seemed to break 
        backwards compatibility with existing objects...
+ "NewDisplayName" - kept as a string but translation keys are replaced with 
                     number based keys
    * EX: "$thing_key1 Iron Sword of $thing_key2" becomes "$1234 Iron Sword of $5678"
+ "LegendaryId" - Integer value, mapped from string
+ "SetId" - Integer value, mapped from string
+ "TypeNameOverride" - String, unchanged
+ "AugmentedEffectIndex" - Integer value, unchanged
+ "AugmentedEffectIndices.Count" - Integer value, unchanged
    * After this, follow the values of the AugmentedEffectIndices
+ "AugmentedEffectIndices[0]" - Integer value, unchanged
...
+ "AugmentedEffectIndices[N]" - Integer value, unchanged
+ "Effects.Count" - Integer value, unchanged
+ "Effects[0].Version" - Integer value, unchanged
+ "Effects[0].EffectType" - Integer value, mapped from string
+ "Effects[0].EffectValue" - Float value, unchanged
...
+ "Effects[N].Version" - Integer value, unchanged
+ "Effects[N].EffectType" - Integer value, mapped from string
+ "Effects[N].EffectValue" - Float value, unchanged


--- An example of how an item would get encoded ---
"{\"Version\":2,\"Rarity\":\"Rare\",\"Effects\":[{\"EffectType\":\"Megingjord\",\"Version\":1,\"EffectValue\":0}],\"TypeNameOverride\":\"$mod_epicloot_belt\",\"AugmentedEffectIndex\":-1,\"AugmentedEffectIndices\":[],\"DisplayName\":null,\"LegendaryID\":null,\"SetID\":null}"
becomes
"2,2,,-1,-1,$mod_epicloot_belt,-1,0,1,1,2,0,"


--- Integer Mapping Definition ---
Four types of string keys are mapped:
+ Translation Keys
+ Legendary IDs
+ Set IDs
+ Magic Effect Types

The values are read from the following files:
"translations.map.json"
"legendaries.map.json"
"legendaries.set.map.json"
"magiceffects.map.json"

    If the files don't exist, they are generated from the existing "translations.json" 
and from "UniqueLegendaryHelper" and "MagicItemEffectDefinitions"


--- Conversion Of Existing Objects and Backwards Compatibility ---
    When deserializing data, if it starts with a bracket character then the JSON 
deserialization is used instead of the CUSTOM one.
    When serializing the data, the CUSTOM serialization is only used if the feature 
has been enabled via configuration file.
    Additionally, also via a configuration file, automatic conversion or reverse
conversion can be performed. This is done by hooking into the static event
"LoadExtendedItemData" from the "ExtendedItemData" class. Upon loading, the 
component data is checked. If CUSTOM serialization is enabled and the encoding is
found to be JSON, then it is converted to CUSTOM and the "Save" method is called. 
If CUSTOM serialization is disabled and the encoding is found to be CUSTOM, then it 
is converted to JSON and the "Save" method is called. 
    Effectively, this means that all the items in a character's inventory will be 
converted to either CUSTOM or JSON depending on if CUSTOM mode is enabled or not.
It should also convert whatever items in the world file get loaded at a given time. 
Unfortunately I'm not sure how to convert *ALL* of the stored items in the world at
once. Perhaps something can be done to loop through all of the existing items in the 
world when the world is first loaded? I am not familiar enough with Valheim modding
to do this yet.
