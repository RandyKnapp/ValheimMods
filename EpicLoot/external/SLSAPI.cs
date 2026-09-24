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
        private static readonly MethodInfo SetCreatureSpawnManagedMethod;

        private static readonly MethodInfo GetPossibleModifiersForType;
        private static readonly MethodInfo GetAllModifiersForCreature;
        private static readonly MethodInfo AddModifierToCreature;

        private static readonly MethodInfo AddNewModifierToSLS;

        // Location Reset
        private static readonly MethodInfo RegisterLocationResetTargetMethod;
        private static readonly MethodInfo UnregisterLocationResetTargetMethod;
        private static readonly MethodInfo GetRegisteredLocationResetTargetsMethod;
        private static readonly MethodInfo GetLocationResetTargetInfoMethod;
        private static readonly MethodInfo IsLocationResetReadyMethod;
        private static readonly MethodInfo GetLocationResetStatusMethod;
        private static readonly MethodInfo IsKnownResetTargetNameMethod;
        private static readonly MethodInfo GetLocationLastResetMethod;
        private static readonly MethodInfo GetSecondsUntilLocationResetMethod;
        private static readonly MethodInfo GetLocationResetInfoMethod;
        private static readonly MethodInfo GetChunkResetInfoMethod;
        private static readonly MethodInfo ResetNamedLocationMethod;
        private static readonly MethodInfo ResetLocationsInRadiusMethod;

        /// <summary>
        /// True when Star Level System is installed at all. This only proves the receiver TYPE
        /// resolved - it says nothing about which methods that build actually has.
        /// </summary>
        public static bool IsAvailable => APIReciever != null;

        /// <summary>
        /// True when the installed Star Level System is new enough to expose the Location Reset API.
        /// Check this rather than IsAvailable before calling anything in that section: a consumer
        /// shipping a newer copy of this file than the installed mod would otherwise call through a
        /// null MethodInfo.
        /// </summary>
        public static bool SupportsLocationReset => ResetNamedLocationMethod != null;

        /// <summary>
        /// True when the installed Star Level System has SetCreatureSpawnManaged. Older builds make that call return false.
        /// </summary>
        public static bool SupportsSpawnManaged => SetCreatureSpawnManagedMethod != null;

        static API() {
            APIReciever = Type.GetType("StarLevelSystem.modules.APIReciever, StarLevelSystem");
            if (APIReciever == null) return;
            UpdateCreatureLevel = APIReciever.GetMethod("UpdateCreatureLevel", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureColorization = APIReciever.GetMethod("UpdateCreatureColorization",  BindingFlags.Public | BindingFlags.Static);
            GetBaseAttributeValue = APIReciever.GetMethod("GetBaseAttributeValue",  BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureBaseAttributes = APIReciever.GetMethod("UpdateCreatureBaseAttributes",  BindingFlags.Public | BindingFlags.Static);
            GetAllBaseAttributeValues = APIReciever.GetMethod("GetAllBaseAttributes", BindingFlags.Public | BindingFlags.Static);
            SetAllBaseAttributeValues = APIReciever.GetMethod("SetAllBaseAttributes", BindingFlags.Public | BindingFlags.Static);
            GetPerLevelAttributeValue = APIReciever.GetMethod("GetPerLevelAttributeValue", BindingFlags.Public | BindingFlags.Static);
            UpdateCreaturePerLevelAttributes = APIReciever.GetMethod("UpdateCreaturePerLevelAttributes", BindingFlags.Public | BindingFlags.Static);
            GetAllPerLevelAttributeValues = APIReciever.GetMethod("GetAllPerLevelAttributes", BindingFlags.Public | BindingFlags.Static);
            SetAllPerLevelAttributeValues = APIReciever.GetMethod("SetAllPerLevelAttributes", BindingFlags.Public | BindingFlags.Static);
            GetCreatureDamageRecievedModifier = APIReciever.GetMethod("GetCreatureDamageRecievedModifier", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureDamageRecievedModifier = APIReciever.GetMethod("UpdateCreatureDamageRecievedModifier", BindingFlags.Public | BindingFlags.Static);
            GetAllDamageRecievedModifiers = APIReciever.GetMethod("GetAllDamageRecievedModifiers", BindingFlags.Public | BindingFlags.Static);
            SetAllDamageRecievedModifiers = APIReciever.GetMethod("SetAllDamageRecievedModifiers", BindingFlags.Public | BindingFlags.Static);
            GetCreatureDamageBonus = APIReciever.GetMethod("GetCreatureDamageBonus", BindingFlags.Public | BindingFlags.Static);
            UpdateCreatureDamageBonus = APIReciever.GetMethod("UpdateCreatureDamageBonus", BindingFlags.Public | BindingFlags.Static);
            GetAllDamageBonus = APIReciever.GetMethod("GetAllDamageBonus", BindingFlags.Public | BindingFlags.Static);
            SetAllDamageBonus = APIReciever.GetMethod("SetAllDamageBonus", BindingFlags.Public | BindingFlags.Static);
            ApplyUpdatesToCreature = APIReciever.GetMethod("ApplyUpdatesToCreature", BindingFlags.Public | BindingFlags.Static);
            SetCreatureSpawnManagedMethod = APIReciever.GetMethod("SetCreatureSpawnManaged", BindingFlags.Public | BindingFlags.Static);
            GetPossibleModifiersForType = APIReciever.GetMethod("GetPossibleModifiersForType", BindingFlags.Public | BindingFlags.Static);
            GetAllModifiersForCreature = APIReciever.GetMethod("GetAllModifiersForCreature", BindingFlags.Public | BindingFlags.Static);
            AddModifierToCreature = APIReciever.GetMethod("AddModifierToCreature", BindingFlags.Public | BindingFlags.Static);
            AddNewModifierToSLS = APIReciever.GetMethod("AddNewModifierToSLS", BindingFlags.Public | BindingFlags.Static);

            RegisterLocationResetTargetMethod = APIReciever.GetMethod("RegisterLocationResetTarget", BindingFlags.Public | BindingFlags.Static);
            UnregisterLocationResetTargetMethod = APIReciever.GetMethod("UnregisterLocationResetTarget", BindingFlags.Public | BindingFlags.Static);
            GetRegisteredLocationResetTargetsMethod = APIReciever.GetMethod("GetRegisteredLocationResetTargets", BindingFlags.Public | BindingFlags.Static);
            GetLocationResetTargetInfoMethod = APIReciever.GetMethod("GetLocationResetTargetInfo", BindingFlags.Public | BindingFlags.Static);
            IsLocationResetReadyMethod = APIReciever.GetMethod("IsLocationResetReady", BindingFlags.Public | BindingFlags.Static);
            GetLocationResetStatusMethod = APIReciever.GetMethod("GetLocationResetStatus", BindingFlags.Public | BindingFlags.Static);
            IsKnownResetTargetNameMethod = APIReciever.GetMethod("IsKnownResetTargetName", BindingFlags.Public | BindingFlags.Static);
            GetLocationLastResetMethod = APIReciever.GetMethod("GetLocationLastReset", BindingFlags.Public | BindingFlags.Static);
            GetSecondsUntilLocationResetMethod = APIReciever.GetMethod("GetSecondsUntilLocationReset", BindingFlags.Public | BindingFlags.Static);
            GetLocationResetInfoMethod = APIReciever.GetMethod("GetLocationResetInfo", BindingFlags.Public | BindingFlags.Static);
            GetChunkResetInfoMethod = APIReciever.GetMethod("GetChunkResetInfo", BindingFlags.Public | BindingFlags.Static);
            ResetNamedLocationMethod = APIReciever.GetMethod("ResetNamedLocation", BindingFlags.Public | BindingFlags.Static);
            ResetLocationsInRadiusMethod = APIReciever.GetMethod("ResetLocationsInRadius", BindingFlags.Public | BindingFlags.Static);
        }

        // Invoke that degrades instead of throwing.
        //
        // The creature methods above call Invoke straight off the field, so a consumer shipping a
        // NEWER copy of this file than the installed Star Level System gets a NullReferenceException
        // at their own call site rather than a usable answer. Everything added from the Location
        // Reset section onward routes through here and returns `fallback` instead.
        private static object Call(MethodInfo method, object fallback, params object[] args) {
            if (method == null) { return fallback; }
            return method.Invoke(null, args);
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
        public static bool SetCreatureLevel(Character creatureId, int newLevel) {
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
        public static bool SetCreatureColorization(Character creatureId, float value, float hue, float sat, bool emission = false) {
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
        public static float GetCreatureBaseAttribute(Character creatureId, int attribute) {
            return (float)GetBaseAttributeValue.Invoke(null, new object[] { creatureId, attribute });
        }

        /// <summary>
        /// This allows setting modifiers to any of a creatures base attributes (this value is applied once, flat addition)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureBaseAttribute(Character creatureId, int attribute, float value) {
            return (bool)UpdateCreatureBaseAttributes.Invoke(null, new object[] { creatureId, attribute, value });
        }

        /// <summary>
        /// Gets all of the creatures base attributes as a dictionary with the key as the enum (BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4)
        /// and the value as the float value of that attribute
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureBaseAttributes(Character creatureId) {
            return (Dictionary<int, float>)GetAllBaseAttributeValues.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Takes a dictionary of all of the creatures base attributes as a dictionary with the key as the enum (BaseHealth = 0, BaseDamage = 1, AttackSpeed = 2, Speed = 3, Size = 4)
        /// and sets their values for the creature
        /// this applies immediately
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures attributes</param>
        /// returns>bool success</returns>
        public static bool SetAllCreatureBaseAttributes(Character creatureId, Dictionary<int, float> attributes) {
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
        public static float GetCreaturePerLevelAttribute(Character creatureId, int attribute) {
            return (float)GetPerLevelAttributeValue.Invoke(null, new object[] { creatureId, attribute });
        }

        /// <summary>
        /// This allows setting modifiers to any of a creatures per level attributes (this value is applied once for every level)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attribute">The enum value of which attribute to get: HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreaturePerLevelAttribute(Character creatureId, int attribute, float value) {
            return (bool)UpdateCreaturePerLevelAttributes.Invoke(null, new object[] { creatureId, attribute, value });
        }

        /// <summary>
        /// Gets all of the creatures per level attributes as a dictionary with the key as the enum (HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4)
        /// and the value as the float value of that attribute
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreaturePerLevelAttributes(Character creatureId) {
            return (Dictionary<int, float>)GetAllPerLevelAttributeValues.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Takes a dictionary of all of the creatures per level attributes as a dictionary with the key as the enum (HealthPerLevel = 0, DamagePerLevel = 1, SpeedPerLevel = 2, AttackSpeedPerLevel = 3, SizePerLevel = 4)
        /// and sets their values for the creature
        /// this applies immediately
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures attributes</param>
        /// returns>bool success</returns>
        public static bool SetAllCreaturePerLevelAttributes(Character creatureId, Dictionary<int, float> attributes) {
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
        public static float GetCreatureDamageReceivedModifier(Character creatureId, int damageType) {
            return (float)GetCreatureDamageRecievedModifier.Invoke(null, new object[] { creatureId, damageType });
        }

        /// <summary>
        /// This allows setting any of a creatures damage received modifiers
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// After a rebuild the persisted value is the base the creature's own modifiers (Resist*, Flame, ...) stack on.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureDamageReceivedModifier(Character creatureId, int damageType, float value) {
            return (bool)UpdateCreatureDamageRecievedModifier.Invoke(null, new object[] { creatureId, damageType, value });
        }

        /// <summary>
        /// Gets all of the creature damage recieved modifiers as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureDamageReceivedModifiers(Character creatureId) {
            return (Dictionary<int, float>)GetAllDamageRecievedModifiers.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Sets all of the creature damage recieved modifiers as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// 1.0 = 100% damage taken, 0.5 = 50% damage taken, 2.0 = 200% damage taken
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// After a rebuild the persisted value is the base the creature's own modifiers (Resist*, Flame, ...) stack on.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of creatures damage recived modifiers</param>
        public static bool SetAllCreatureDamageReceivedModifiers(Character creatureId, Dictionary<int, float> attributes) {
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
        public static float GetCreatureFlatDamageBonus(Character creatureId, int damageType) {
            return (float)GetCreatureDamageBonus.Invoke(null, new object[] { creatureId, damageType });
        }

        /// <summary>
        /// Allows setting flat damage bonus values for a creature (this value is applied once, flat addition)
        /// this does not apply immediately and must be applied with ApplyCreatureUpdates
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// After a rebuild the persisted value is the base the creature's own modifiers (Resist*, Flame, ...) stack on.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="damageType">The enum value of which attribute to get: Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9</param>
        /// <param name="value">The value this attribute will be set to (overrides existing)</param>
        /// returns>bool success</returns>
        public static bool SetCreatureFlatDamageBonus(Character creatureId, int damageType, float value) {
            return (bool)UpdateCreatureDamageBonus.Invoke(null, new object[] { creatureId, damageType, value });
        }

        /// <summary>
        /// Gets all of the creatures flat damage bonuses as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<int, float></returns>
        public static Dictionary<int, float> GetAllCreatureFlatDamageBonuses(Character creatureId) {
            return (Dictionary<int, float>)GetAllDamageBonus.Invoke(null, new object[] { creatureId });
        }

        /// <summary>
        /// Sets all of the creatures flat damage bonuses as a dictionary with the key as the enum (Blunt = 0, Slash = 1, Pierce = 2, Fire = 3, Frost = 4, Lightning = 5, Poison = 6, Spirit = 7, Chop = 8, Pickaxe = 9)
        /// this applies immediately
        /// </summary>
        /// <remarks>
        /// Persisted on the creature when called by its ZDO owner, so the value survives a reload, an ownership
        /// handoff and a Star Level System config reload, and every other peer sees it. A call from a peer that does
        /// not own the creature changes only that peer's view.
        /// After a rebuild the persisted value is the base the creature's own modifiers (Resist*, Flame, ...) stack on.
        /// </remarks>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="attributes">Dictionary<int, float> of all creatures flat damage bonuses</param>
        /// returns>bool success</returns>
        public static bool SetAllCreatureFlatDamageBonuses(Character creatureId, Dictionary<int, float> attributes) {
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
        public static bool ApplyCreatureUpdates(Character creatureId) {
            return (bool)ApplyUpdatesToCreature.Invoke(null, new object[] { creatureId });
        }

        ////////////////////////////////////////
        /// SPAWN MANAGEMENT
        ////////////////////////////////////////

        /// <summary>
        /// Marks a creature your mod spawned and owns the existence and level of (a quest or bounty target, say).
        /// Star Level System keeps giving it stats, modifiers and colour, but never deletes or multiplies it for
        /// spawn-rate or disabled-spawn rules, and never rerolls or clamps its level.
        /// Only the creature's ZDO owner can set this. Call it in the frame you spawn the creature (Star Level System's
        /// own setup waits InitialDelayBeforeSetup), and again when it loads if it may have been spawned before your
        /// mod made this call.
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// <param name="managed">True to mark the creature, false to hand it back to Star Level System's spawn rules</param>
        /// returns>bool success; false when this peer does not own the creature or Star Level System is too old (see SupportsSpawnManaged)</returns>
        public static bool SetCreatureSpawnManaged(Character creatureId, bool managed = true) {
            return (bool)Call(SetCreatureSpawnManagedMethod, false, creatureId, managed);
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
        public static List<string> GetPossibleModifiers(int modType = 0) {
            return (List<string>)GetPossibleModifiersForType.Invoke(null, new object[] { modType });
        }

        /// <summary>
        /// Gets all of the modifiers currently applied to a creature
        /// Dictionary key = modifier name, value = modifier type
        /// </summary>
        /// <param name="creatureId">The creature's Character class</param>
        /// returns>Dictionary<string, int> of modifier names and type int</returns>
        public static Dictionary<string, int> GetCreaturesModifiers(Character creatureId) {
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
        public static bool AddModifierToTargetCreature(Character creatureId, string modifierName, int modifierType, bool update = true) {
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
            Delegate setupMethod = null,
            float selectionWeight = 10f,
            float basepower = 0f,
            float perlevelpower = 0f,
            Dictionary<Heightmap.Biome, List<string>> biomeConfig = null,
            int namingStyle = 2,
            string name_suffixes = null,
            string name_prefixes = null,
            int visualStyle = 0,
            Sprite starIcon = null,
            GameObject visualEffect = null,
            List<string> allowed_creatures = null,
            List<string> unallowed_creatures = null,
            List<Heightmap.Biome> allowed_biomes = null) {
            return (bool)AddNewModifierToSLS.Invoke(null, new object[] { modifierID, modifier_name, setupMethod, selectionWeight, basepower, perlevelpower, biomeConfig, namingStyle,
                name_suffixes, name_prefixes, visualStyle, starIcon, visualEffect, allowed_creatures, unallowed_creatures, allowed_biomes });
        }

        ////////////////////////////////////////
        /// LOCATION RESET
        ////////////////////////////////////////
        ///
        /// Star Level System can restore looted locations, dungeons, ore, pickables and vegetation on
        /// a schedule. This section lets your mod register its own targets for that sweep, ask for a
        /// reset directly, and find out when something was last reset.
        ///
        /// ALL OF IT WORKS FROM A CLIENT. The work happens on the server - it owns the world objects -
        /// but calling from a client relays the request and brings the answer back, so an item that
        /// resets a dungeon can run its logic on whoever used it.
        ///
        /// That is why every method here takes a callback and returns bool rather than returning the
        /// answer directly: on a client the answer arrives over the network a moment later.
        ///
        /// THE CALLBACK ALWAYS FIRES, EXACTLY ONCE - on success, on a deferral, on a refusal, on a
        /// timeout, and when there is no server to ask. Refusals arrive as a result carrying
        /// "outcome" = "refused", a human-readable "reason" and a machine-readable "refusalCode", so
        /// your follow-up logic lives in the callback and nowhere else. The bool return only tells
        /// you whether the work started; when it is false the reason has already been delivered.
        ///
        /// On the server, and for anything refused before it leaves this machine, the callback runs
        /// before the call returns.
        ///
        /// Guard on SupportsLocationReset, not just IsAvailable.

        /// <summary>
        /// Register a location or vegetation prefab as a recurring reset target. It joins Star Level
        /// System's normal background sweep exactly as a configured target would, timed off the
        /// location's own world data, and survives config reloads and world reloads.
        ///
        /// Safe to call at any point, including before a world is loaded, if you are the server. From
        /// a client this relays, so it needs a live server connection - register on connect rather
        /// than in your Awake if your mod is client-side.
        ///
        /// The server owner's LocationResetSettings.yaml always wins: if they add an entry for this
        /// prefab name, their settings replace yours entirely. Protection rules (what player-built
        /// content blocks a reset) are theirs alone and cannot be set from here.
        /// </summary>
        /// <param name="prefabName">Location or vegetation prefab name, e.g. "Crypt2"</param>
        /// <param name="sourceId">Your plugin GUID. Used in logs and to scope unregistration</param>
        /// <param name="resetHours">Hours between resets. 0 uses the server's default interval. Minimum 0.25</param>
        /// <param name="resetSchedule">A 5-field cron expression instead of an interval, e.g. "0 3 * * *". Wins over resetHours</param>
        /// <param name="enabled">Whether the target is active</param>
        /// <param name="mode">0 = Full (clear and rebuild), 1 = TerrainOnly (undo terraforming, leave the location)</param>
        /// <param name="resetTerrain">Also revert player terraforming around the location</param>
        /// <param name="terrainRadius">Terrain reset radius. 0 uses the location's own exterior radius</param>
        /// <param name="extraTerrainRadius">Extra metres of terrain reset beyond that radius</param>
        /// <param name="resetInterior">Whether a dungeon interior may be regenerated. False leaves such locations alone entirely</param>
        /// <param name="minDistance">Only apply beyond this distance from the world's reset centre. 0 = no limit</param>
        /// <param name="maxDistance">Only apply within this distance. 0 = no limit</param>
        /// <param name="onResult">Receives whether the server accepted the registration</param>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool RegisterLocationReset(string prefabName, string sourceId,
            float resetHours = 0f, string resetSchedule = null, bool enabled = true, int mode = 0,
            bool resetTerrain = false, float terrainRadius = 0f, float extraTerrainRadius = 0f,
            bool resetInterior = true, float minDistance = 0f, float maxDistance = 0f,
            Action<bool> onResult = null) {
            return (bool)Call(RegisterLocationResetTargetMethod, false,
                prefabName, sourceId, resetHours, resetSchedule, mode, resetTerrain, terrainRadius,
                extraTerrainRadius, resetInterior, minDistance, maxDistance, enabled, onResult);
        }

        /// <summary>
        /// Remove a target you registered. Refused if another mod owns that registration.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool UnregisterLocationReset(string prefabName, string sourceId, Action<bool> onResult = null) {
            return (bool)Call(UnregisterLocationResetTargetMethod, false, prefabName, sourceId, onResult);
        }

        /// <summary>
        /// Every prefab name registered through this API. Pass your plugin GUID to see only your own,
        /// or null for all of them.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetRegisteredLocationResets(string sourceId, Action<List<string>> onResult) {
            return (bool)Call(GetRegisteredLocationResetTargetsMethod, false, sourceId, onResult);
        }

        /// <summary>
        /// How a reset target actually resolved, whoever registered it. Keys include: name, known,
        /// configured, enabled, source ("entry", "group", "api", "defaults", "none"), registeredBy,
        /// hardBlocked, isLocation, isVegetation, resetSeconds, schedule, mode, resetTerrain,
        /// terrainRadius, extraTerrainRadius, resetInterior.
        ///
        /// "source" is the one to check if your registration does not seem to be taking effect:
        /// anything other than "api" means the server's config is overriding it.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetLocationResetTargetInfo(string prefabName, Action<Dictionary<string, object>> onResult) {
            return (bool)Call(GetLocationResetTargetInfoMethod, false, prefabName, onResult);
        }

        /// <summary>
        /// Whether a reset request would currently be accepted by the server: a world is loaded and
        /// no conflicting reset mod is installed.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool IsLocationResetReady(Action<bool> onResult) {
            return (bool)Call(IsLocationResetReadyMethod, false, onResult);
        }

        /// <summary>
        /// Location Reset state on the server. Keys include: available, isServer, ready, sweepAllowed,
        /// masterSwitchEnabled, configEnabled, blockedByModConflict, resetRunning, trackedZones,
        /// generatedZones, sweepFloorSeconds, apiRegistrations, and the limits applied to client
        /// requests: clientMaxRadius, clientMaxDistance, clientCooldownSeconds.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetLocationResetStatus(Action<Dictionary<string, object>> onResult) {
            return (bool)Call(GetLocationResetStatusMethod, false, onResult);
        }

        /// <summary>
        /// Whether this world has any location, vegetation entry or prefab by that name. Use it to
        /// catch a prefab name that a game update renamed out from under you.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool IsKnownLocationResetTarget(string prefabName, Action<bool> onResult) {
            return (bool)Call(IsKnownResetTargetNameMethod, false, prefabName, onResult);
        }

        /// <summary>
        /// When the named location nearest `center` was last reset, as Unix seconds UTC.
        /// The callback receives -1 if no location of that name is within `radius` (or it has nothing
        /// to time from), and 0 if it exists but has never been reset.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetLocationLastReset(string locationName, Vector3 center, float radius, Action<long> onResult) {
            return (bool)Call(GetLocationLastResetMethod, false, locationName, center, radius, onResult);
        }

        /// <summary>
        /// Seconds until the named location is next due for a reset. The callback receives 0 for due
        /// now, and -1 for unknown - not found, never reset, or nothing has it configured.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetSecondsUntilLocationReset(string locationName, Vector3 center, float radius, Action<double> onResult) {
            return (bool)Call(GetSecondsUntilLocationResetMethod, false, locationName, center, radius, onResult);
        }

        /// <summary>
        /// Full reset state for the named location nearest `center`. Keys include: found, name,
        /// zoneX, zoneZ, positionX/Y/Z, distance, configured, enabled, hardBlocked, source,
        /// groupName, schedule, mode, resetTerrain, resetInterior, hasProxy, lastResetUnix,
        /// secondsSinceReset, secondsUntilDue, dueNow, rateMultiplier, rateDescription.
        ///
        /// Always check "found" first.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetLocationResetInfo(string locationName, Vector3 center, float radius,
            Action<Dictionary<string, object>> onResult) {
            return (bool)Call(GetLocationResetInfoMethod, false, locationName, center, radius, onResult);
        }

        /// <summary>
        /// Reset state for the map chunk containing `position`. Keys include: zoneX, zoneZ, centerX,
        /// centerZ, biome, generated, loaded, tracked, lastExaminedUnix, secondsSinceExamined,
        /// deferredUntilUnix, retryAtUnix, retryCount, rateMultiplier, rateDescription,
        /// protectionBlocked, protectionReason, locationName, locationLastResetUnix,
        /// locationSecondsUntilDue.
        ///
        /// Note that lastExaminedUnix is when the sweep last LOOKED at this chunk, which is not the
        /// same as anything in it having been reset - for that, read locationLastResetUnix or use
        /// GetLocationLastReset. When the chunk has been deferred (a player build is blocking it, for
        /// instance) lastExaminedUnix is 0 and deferredUntilUnix carries the time it comes back up.
        /// </summary>
        /// <param name="includePrefabs">Also return a "prefabs" list of per-prefab census rows
        /// (name, lastResetUnix, baseline, live). This costs a full scan of the chunk</param>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool GetChunkResetInfo(Vector3 position, bool includePrefabs,
            Action<Dictionary<string, object>> onResult) {
            return (bool)Call(GetChunkResetInfoMethod, false, position, includePrefabs, onResult);
        }

        /// <summary>
        /// Reset the named location nearest `center`, within `radius`.
        ///
        /// Unlike the background sweep this works on a location the server has not configured for
        /// resets at all - you named it, so it is reset. Locations that can never be reset (the
        /// starting temple) are still refused.
        ///
        /// SAFETY. safety 0 (Safe, the default) waits for players to leave the affected chunks before
        /// touching anything, and gives up without resetting if they do not. safety 1 (Force) resets
        /// immediately, working on chunks that are loaded around a player - use this when you want a
        /// location restored before somebody walks back into it. Player-built structures block a
        /// reset in BOTH modes and there is no way to override that.
        ///
        /// Be careful with Force on a dungeon somebody is inside: interiors are rebuilt from scratch
        /// and a player standing in one will fall.
        ///
        /// CALLING FROM A CLIENT. The server clamps the radius, refuses a position further away than
        /// its configured limit, and rate-limits how often any one client may ask. Read
        /// clientMaxRadius / clientMaxDistance / clientCooldownSeconds from GetLocationResetStatus to
        /// size your requests. Every refusal reaches onComplete with a refusalCode - "too_far",
        /// "cooldown", "no_such_location", "already_running" and so on - so a caller can retry,
        /// explain, or refund without guessing why.
        /// </summary>
        /// <param name="locationName">Location prefab name, e.g. "Crypt2"</param>
        /// <param name="center">World position to search around</param>
        /// <param name="radius">Search radius in metres</param>
        /// <param name="safety">0 = Safe (wait for players), 1 = Force (reset now)</param>
        /// <param name="resetAllMatches">Reset every match in range rather than just the nearest</param>
        /// <param name="safeWaitSeconds">How long Safe waits before giving up. 0 uses the default (300s)</param>
        /// <param name="includeDetail">Include a per-chunk "zones" list in the result</param>
        /// <param name="onComplete">Called exactly once: with the result summary when the reset
        /// finishes, or with a refusal carrying outcome/reason/refusalCode if it never ran</param>
        /// <returns>bool - whether the reset started. False means onComplete has already been given
        /// the reason</returns>
        public static bool ResetNamedLocation(string locationName, Vector3 center, float radius = 128f,
            int safety = 0, bool resetAllMatches = false, float safeWaitSeconds = 0f,
            bool includeDetail = false, Action<Dictionary<string, object>> onComplete = null) {
            return (bool)Call(ResetNamedLocationMethod, false,
                locationName, center, radius, safety, resetAllMatches, safeWaitSeconds, includeDetail, onComplete);
        }

        /// <summary>
        /// Reset everything the server has configured for resets within `radius` of `center`. Same
        /// safety rules and same client limits as ResetNamedLocation.
        /// </summary>
        /// <returns>bool - whether the request was dispatched</returns>
        public static bool ResetLocationsInRadius(Vector3 center, float radius = 128f, int safety = 0,
            float safeWaitSeconds = 0f, bool includeDetail = false,
            Action<Dictionary<string, object>> onComplete = null) {
            return (bool)Call(ResetLocationsInRadiusMethod, false,
                center, radius, safety, safeWaitSeconds, includeDetail, onComplete);
        }
    }
}
