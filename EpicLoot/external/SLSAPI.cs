using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StarLevelSystem
{

    [PublicAPI]
    public static class API
    {
        private static readonly Type APIReciever;
        private static readonly MethodInfo UpdateCreatureLevel;
        private static readonly MethodInfo UpdateCreatureColorization;

        private static readonly MethodInfo GetBaseAttributeValue;
        private static readonly MethodInfo UpdateCreatureBaseAttributes;
        private static readonly MethodInfo GetAllBaseAttributeValues;
        private static readonly MethodInfo SetAllBaseAttributeValues;

        private static readonly MethodInfo GetPerLevelAttributeValue;
        private static readonly MethodInfo UpdateCreaturePerLevelAttributes;
        private static readonly MethodInfo GetAllPerLevelAttributeValues;
        private static readonly MethodInfo SetAllPerLevelAttributeValues;

        private static readonly MethodInfo GetCreatureDamageRecievedModifier;
        private static readonly MethodInfo UpdateCreatureDamageRecievedModifier;
        private static readonly MethodInfo GetAllDamageRecievedModifiers;
        private static readonly MethodInfo SetAllDamageRecievedModifiers;

        private static readonly MethodInfo GetCreatureDamageBonus;
        private static readonly MethodInfo UpdateCreatureDamageBonus;
        private static readonly MethodInfo GetAllDamageBonus;
        private static readonly MethodInfo SetAllDamageBonus;

        private static readonly MethodInfo ApplyUpdatesToCreature;

        private static readonly MethodInfo GetPossibleModifiersForType;
        private static readonly MethodInfo GetAllModifiersForCreature;
        private static readonly MethodInfo AddModifierToCreature;

        private static readonly MethodInfo AddNewModifierToSLS;

        public static bool IsAvailable => APIReciever != null;

        static API()
        {
            APIReciever = Type.GetType("StarLevelSystem.modules.APIReciever, StarLevelSystem");
            if (APIReciever == null) return;
            UpdateCreatureLevel = APIReciever.GetMethod("UpdateCreatureLevel", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureColorization = APIReciever.GetMethod("UpdateCreatureColorization", BindingFlags.Public | BindingFlags.Static);
            GetBaseAttributeValue = APIReciever.GetMethod("GetBaseAttributeValue", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureBaseAttributes = APIReciever.GetMethod("UpdateCreatureBaseAttributes", BindingFlags.Public | BindingFlags.Static);
            GetAllBaseAttributeValues = APIReciever.GetMethod("GetAllBaseAttributeValues", BindingFlags.Public | BindingFlags.Static);
            SetAllBaseAttributeValues = APIReciever.GetMethod("SetAllBaseAttributeValues", BindingFlags.Public | BindingFlags.Static);
            GetPerLevelAttributeValue = APIReciever.GetMethod("GetPerLevelAttributeValue", BindingFlags.Public | BindingFlags.Static);
            UpdateCreaturePerLevelAttributes = APIReciever.GetMethod("UpdateCreaturePerLevelAttributes", BindingFlags.Public | BindingFlags.Static);
            GetAllPerLevelAttributeValues = APIReciever.GetMethod("GetAllPerLevelAttributeValues", BindingFlags.Public | BindingFlags.Static);
            SetAllPerLevelAttributeValues = APIReciever.GetMethod("SetAllPerLevelAttributeValues", BindingFlags.Public | BindingFlags.Static);
            GetCreatureDamageRecievedModifier = APIReciever.GetMethod("GetCreatureDamageRecievedModifier", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureDamageRecievedModifier = APIReciever.GetMethod("UpdateCreatureDamageRecievedModifier", BindingFlags.Public | BindingFlags.Static);
            GetAllDamageRecievedModifiers = APIReciever.GetMethod("GetAllDamageRecievedModifiers", BindingFlags.Public | BindingFlags.Static);
            SetAllDamageRecievedModifiers = APIReciever.GetMethod("SetAllDamageRecievedModifiers", BindingFlags.Public | BindingFlags.Static);
            GetCreatureDamageBonus = APIReciever.GetMethod("GetCreatureDamageBonus", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureDamageBonus = APIReciever.GetMethod("UpdateCreatureDamageBonus", BindingFlags.Public | BindingFlags.Static);
            GetAllDamageBonus = APIReciever.GetMethod("GetAllDamageBonus", BindingFlags.Public | BindingFlags.Static);
            SetAllDamageBonus = APIReciever.GetMethod("SetAllDamageBonus", BindingFlags.Public | BindingFlags.Static);
            ApplyUpdatesToCreature = APIReciever.GetMethod("ApplyUpdatesToCreature", BindingFlags.Public | BindingFlags.Static);
            GetPossibleModifiersForType = APIReciever.GetMethod("GetPossibleModifiersForType", BindingFlags.Public | BindingFlags.Static);
            GetAllModifiersForCreature = APIReciever.GetMethod("GetAllModifiersForCreature", BindingFlags.Public | BindingFlags.Static);
            AddModifierToCreature = APIReciever.GetMethod("AddModifierToCreature", BindingFlags.Public | BindingFlags.Static);
            AddNewModifierToSLS = APIReciever.GetMethod("AddNewModifierToSLS", BindingFlags.Public | BindingFlags.Static);
        }

        /////////////////////
        /// LEVEL
        /////////////////////

        /// <summary>
        /// Sets the creatures level, this applies immediately.
        /// If you want the creature to be resized to its new level, you must call ApplyCreatureUpdates after this.
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="newLevel">The new level to set the creature to</param>
        /// returns>bool success</returns>
        public static bool SetCreatureLevel(Character creatureId, int newLevel)
        {
            return (bool)UpdateCreatureLevel.Invoke(null, new object[] { creatureId, newLevel });
        }

        /////////////////////
        /// COLORIZATION
        /////////////////////

        /// <summary>
        /// Set the creatures colorization, this applies immediately.
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="value">The enum value of which attribute to get: BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4</param>
        /// <param name="value">unity material value</param>
        /// <param name="hue">unity material hue</param>
        /// <param name="sat">unity material saturation</param>
        /// <param name="emission">enables emission on the material, if enabled, the emissive color will be value, hue, saturation</param>
        /// returns>bool success</returns>
        public static bool SetCreatureColorization(Character creatureId, float value, float hue, float sat, bool emission = false)
        {
            return (bool)UpdateCreatureColorization.Invoke(null, new object[] { creatureId, value, hue, sat, emission });
        }

        //////////////////////////////
        /// BASE CREATURE ATTRIBUTES
        //////////////////////////////

        /// <summary>
        /// This allows retrieving any of a creatures base attributes
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4</param>
        /// returns>float value of the base attribute</returns>
        public static float GetCreatureBaseAttribute(Character creatureId, int attribute)
        {
            return (float)GetBaseAttributeValue.Invoke(null, new object[] { creatureId, attribute });
        }

        /// <summary>
        /// This allows setting modifiers to any of a creatures base attributes (this value is applied once, flat addition)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureBaseAttribute(Character creatureId, int attribute, float value)
        {
            return (bool)UpdateCreatureBaseAttributes.Invoke(null, new object[] { creatureId, attribute, value });
        }

        /// <summary>
        /// Gets all of the creatures base attributes as a dictionary with the key as the enum (BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4)
        /// and the value as the float value of that attribute
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureBaseAttributes(Character creatureId)
        {
            return (Dictionary<int, float>)GetAllBaseAttributeValues.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Takes a dictionary of all of the creatures base attributes as a dictionary with the key as the enum (BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4)
        /// and sets their values for the creature
        /// this applies immediately
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures attributes</param>
        /// returns>bool success</returns>
        public static bool SetAllCreatureBaseAttributes(Character creatureId, Dictionary<int, float> attributes)
        {
            return (bool)SetAllBaseAttributeValues.Invoke(null, new object[] { creatureId, attributes });
        }

        ////////////////////////////////////
        /// PER LEVEL CREATURE ATTRIBUTES
        ////////////////////////////////////

        /// <summary>
        /// This allows retrieving any of a creatures per level attributes
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4</param>
        /// returns>float value of the per level attribute</returns>
        public static float GetCreaturePerLevelAttribute(Character creatureId, int attribute)
        {
            return (float)GetPerLevelAttributeValue.Invoke(null, new object[] { creatureId, attribute });
        }

        /// <summary>
        /// This allows setting modifiers to any of a creatures per level attributes (this value is applied once for every level)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreaturePerLevelAttribute(Character creatureId, int attribute, float value)
        {
            return (bool)UpdateCreaturePerLevelAttributes.Invoke(null, new object[] { creatureId, attribute, value });
        }

        /// <summary>
        /// Gets all of the creatures per level attributes as a dictionary with the key as the enum (HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4)
        /// and the value as the float value of that attribute
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreaturePerLevelAttributes(Character creatureId)
        {
            return (Dictionary<int, float>)GetAllPerLevelAttributeValues.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Takes a dictionary of all of the creatures per level attributes as a dictionary with the key as the enum (HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4)
        /// and sets their values for the creature
        /// this applies immediately
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures attributes</param>
        /// returns>bool success</returns>
        public static bool SetAllCreaturePerLevelAttributes(Character creatureId, Dictionary<int, float> attributes)
        {
            return (bool)SetAllPerLevelAttributeValues.Invoke(null, new object[] { creatureId, attributes });
        }

        ////////////////////////////////////////
        /// CREATURE DAMAGE RECEIVED MODIFIERS
        ////////////////////////////////////////

        /// <summary>
        /// This allows retrieving any of a creatures damage received modifiers
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// returns>float value damage recieved modifier</returns>
        public static float GetCreatureDamageReceivedModifier(Character creatureId, int damageType)
        {
            return (float)GetCreatureDamageRecievedModifier.Invoke(null, new object[] { creatureId, damageType });
        }

        /// <summary>
        /// This allows setting any of a creatures damage received modifiers
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureDamageReceivedModifier(Character creatureId, int damageType, float value)
        {
            return (bool)UpdateCreatureDamageRecievedModifier.Invoke(null, new object[] { creatureId, damageType, value });
        }

        /// <summary>
        /// Gets all of the creature damage recieved modifiers as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureDamageReceivedModifiers(Character creatureId)
        {
            return (Dictionary<int, float>)GetAllDamageRecievedModifiers.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Sets all of the creature damage recieved modifiers as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of creatures damage recived modifiers</param>
        public static bool SetAllCreatureDamageReceivedModifiers(Character creatureId, Dictionary<int, float> attributes)
        {
            return (bool)SetAllDamageRecievedModifiers.Invoke(null, new object[] { creatureId, attributes });
        }

        ////////////////////////////////////////
        /// CREATURE DAMAGE BONUSES
        ////////////////////////////////////////

        /// <summary>
        /// Allows retreiving damage bonuses values for a creature
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// returns>float value of the base attribute</returns>
        public static float GetCreatureFlatDamageBonus(Character creatureId, int damageType)
        {
            return (float)GetCreatureDamageBonus.Invoke(null, new object[] { creatureId, damageType });
        }

        /// <summary>
        /// Allows setting flat damage bonus values for a creature (this value is applied once, flat addition)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureFlatDamageBonus(Character creatureId, int damageType, float value)
        {
            return (bool)UpdateCreatureDamageBonus.Invoke(null, new object[] { creatureId, damageType, value });
        }

        /// <summary>
        /// Gets all of the creatures flat damage bonuses as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureFlatDamageBonuses(Character creatureId)
        {
            return (Dictionary<int, float>)GetAllDamageBonus.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Sets all of the creatures flat damage bonuses as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// this applies immediately
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures flat damage bonuses</param>
        /// returns>bool success</returns>
        public static bool SetAllCreatureFlatDamageBonuses(Character creatureId, Dictionary<int, float> attributes)
        {
            return (bool)SetAllDamageBonus.Invoke(null, new object[] { creatureId, attributes });
        }


        ////////////////////////////////////////
        /// APPLY ALL STAT CHANGES
        ////////////////////////////////////////

        /// <summary>
        /// Applies DamageBonuses, PerLevel, BaseAttributes, speed, size, health, damage, etc to the creature
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>bool success</returns>
        public static bool ApplyCreatureUpdates(Character creatureId)
        {
            return (bool)ApplyUpdatesToCreature.Invoke(null, new object[] { creatureId });
        }

        ////////////////////////////////////////
        /// Creature Modifier Management
        ////////////////////////////////////////

        /// <summary>
        /// Gets all of the loaded modifiers that are available for a particular type
        /// types = 0 = Major, 1 = Minro, 2 = Boss
        /// </summary>
        /// <param name="modType">The type of modifier to get (types = 0 = Major, 1 = Minro, 2 = Boss)</param>
        /// returns>List<string> of modifier names</returns>
        public static List<string> GetPossibleModifiers(int modType = 0)
        {
            return (List<string>)GetPossibleModifiersForType.Invoke(null, new object[] { modType });
        }

        /// <summary>
        /// Gets all of the modifiers currently applied to a creature
        /// Dictionary key = modifier name, value = modifier type
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<string, int> of modifier names and type int</returns>
        public static Dictionary<string, int> GetCreaturesModifiers(Character creatureId)
        {
            return (Dictionary<string, int>)GetAllModifiersForCreature.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Adds a modifier to a creature, by default this applies immediately
        /// Requires a valid modifer name, which can be retrieved with GetPossibleModifiers
        /// Modifiers are sorted into 3 types, Major (0), Minor (1), and Boss (2)
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="modifierName">The modifiers name</param>
        /// <param name="modifierType">The modifiers type Major (0), Minor (1), and Boss (2)</param>
        /// <param name="update">If true applies updates to the creature to rebuild the creatures name and other stats</param>
        /// returns>bool success</returns>
        public static bool AddModifierToTargetCreature(Character creatureId, string modifierName, int modifierType, bool update = true)
        {
            return (bool)AddModifierToCreature.Invoke(null, new object[] { creatureId, modifierName, modifierType, update });
        }

        ////////////////////////////////////////
        /// Modifier Creation | In-development
        ////////////////////////////////////////


        /// <summary>
        /// Add a new modifier to Star Level System
        /// This modifier can then be applied to creatures using the API, or it can be randomly applied based on the configuration parameters
        /// Every modifier of a particular type with a weight that is above 0 has a chance to be applied when a creature selects that type of modifier
        /// <param name="modifierID">[Required] An integer ID for the modifier, must be unique, used to save and load this modifier</param>
        /// <param name="modifier_name">[Required] The name of the modifier, must be unique, used to reference the config for this modifier</param>
        /// <param name="setupMethod">The name of the class containing a Setup method (if needed) Should specify the assembly name. eg: Midnight.TestPluginClass, TestPlugin </param>
        /// <param name="selectionWeight">The weight of this modifier when randomly selecting modifiers, higher weights increase the chance of being selected</param>
        /// <param name="basepower">The base power of this modifier, this is applied once</param>
        /// <param name="perlevelpower">The per level power of this modifier, this is applied once for each level of the creature.</param>
        /// <param name="biomeConfig">A dictionary of biomes with string values, optional configuration specific to each biome for this modifier.</param>
        /// <param name="namingStyle">The naming style of this modifier, 0 = prefix only, 1 = suffix only, 2 = prefix and suffix</param>
        /// <param name="name_suffixes">A list of suffixes to use for this modifier, can be localized</param>
        /// <param name="name_prefixes">A list of prefixes to use for this modifier, can be localized</param>
        /// <param name="visualStyle">At what point visual effects are attached to the creature</param>
        /// <param name="starIcon">The icon to use for this modifier in the star display</param>
        /// <param name="visualEffect">The visual effect to attach to the creature </param>
        /// <param name="allowed_creatures">A list of creature names that this modifier can be applied to, if null or empty, all creatures are allowed</param>
        /// <param name="unallowed_creatures">A list of creature names that this modifier will not be applied to, if null or empty, no creatures are excluded</param>
        /// returns>bool success</returns>
        public static bool AddNewModifier(
            int modifierID,
            string modifier_name,
            string setupMethod = null,
            float selectionWeight = 10f,
            float basepower = 0f,
            float perlevelpower = 0f,
            Dictionary<Heightmap.Biome, List<string>> biomeConfig = null,
            int namingStyle = 2,
            List<string> name_suffixes = null,
            List<string> name_prefixes = null,
            int visualStyle = 0,
            Sprite starIcon = null,
            GameObject visualEffect = null,
            List<string> allowed_creatures = null,
            List<string> unallowed_creatures = null,
            List<Heightmap.Biome> allowed_biomes = null)
        {
            return (bool)AddNewModifierToSLS.Invoke(null, new object[] { modifierID, modifier_name, setupMethod, selectionWeight, basepower, perlevelpower, biomeConfig, namingStyle,
                name_suffixes, name_prefixes, visualStyle, starIcon, visualEffect, allowed_creatures, unallowed_creatures, allowed_biomes });
        }
    }
}
