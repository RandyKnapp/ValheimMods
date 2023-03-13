using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.Data
{
    #nullable enable
	[PublicAPI]
	public abstract class CustomItemData
	{
		public string CustomDataKey { get; private set; } = null!;

		protected virtual bool AllowStackingIdenticalValues { get; set; } = true;

		public string Value
		{
			get => Item.m_customData.TryGetValue(CustomDataKey, out string data) ? data : "";
			set => Item.m_customData[CustomDataKey] = value;
		}

		private string key = null!;

		public string Key
		{
			get => key;
			internal set
			{
				key = value;
				CustomDataKey = ItemInfo.dataKey(ItemInfo.classKey(GetType(), Key));
			}
		}

		public bool IsCloned => Info.isCloned.Contains(CustomDataKey);
		public bool IsAlive => info.TryGetTarget(out _);

		public ItemDrop.ItemData Item => Info.ItemData;
		internal WeakReference<ItemInfo> info = null!;
		public ItemInfo Info => info.TryGetTarget(out ItemInfo itemInfo) ? itemInfo : new ItemInfo(new ItemDrop.ItemData());

		public virtual void FirstLoad() { }
		public virtual void Load() { }
		public virtual void Save() { }
		public virtual void Unload() { }
		public virtual void Upgraded() { }

		// data arg is CustomItemData this CustomItemData is stacked with (identical Key) - if the other item has no such CustomItemData, null is passed
		// If null, stacking disallowed.
		// If non-null, the new item will have CustomItemData with this new string-value
		// By default stacking is disallowed. Set AllowStackingIdenticalValues property to true for trivial by Value comparisons.
		public virtual string? TryStack(CustomItemData? data) => AllowStackingIdenticalValues && data?.Value == Value ? Value : null;
	}

	public sealed class StringItemData : CustomItemData
	{
	}

	[PublicAPI]
	public class ItemInfo : IEnumerable<CustomItemData>
	{
		public static HashSet<Type> ForceLoadTypes = new();

		internal static string? _modGuid;

		internal static string modGuid => _modGuid ??= ((Func<string>)(() =>
		{
			IEnumerable<TypeInfo> types;
			try
			{
				types = Assembly.GetExecutingAssembly().DefinedTypes.ToList();
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
			}
			BaseUnityPlugin plugin = (BaseUnityPlugin)Chainloader.ManagerObject.GetComponent(types.First(t => t.IsClass && typeof(BaseUnityPlugin).IsAssignableFrom(t)));
			return plugin.Info.Metadata.GUID;
		}))();

		private static Dictionary<Type, HashSet<Type>> typeInheritorsCache = new();
		private static HashSet<string> knownTypes = new();

		public string Mod => modGuid;
		public ItemDrop.ItemData ItemData { get; private set; }

		private Dictionary<string, CustomItemData> data = new();
		private WeakReference<ItemInfo>? selfReference = null;

		internal HashSet<string> isCloned = new();

		internal static void addTypeToInheritorsCache(Type type, string typeKey)
		{
			if (!knownTypes.Contains(typeKey))
			{
				void AddInterfaces(Type baseType)
				{
					if (!typeInheritorsCache.TryGetValue(baseType, out HashSet<Type> itemDataTypes))
					{
						itemDataTypes = typeInheritorsCache[baseType] = new HashSet<Type>();
					}

					itemDataTypes.Add(type);
					foreach (Type iface in baseType.GetInterfaces())
					{
						AddInterfaces(iface);
					}
				}

				knownTypes.Add(typeKey);
				Type? baseType = type;
				while (baseType is not null)
				{
					AddInterfaces(baseType);
					baseType = baseType.BaseType;
				}
			}
		}

		internal static string classKey(Type type, string key)
		{
			string typeKey = type.FullName + (type.Assembly != Assembly.GetExecutingAssembly() ? $",{type.Assembly.GetName().Name}" : "");
			addTypeToInheritorsCache(type, typeKey);
			return typeKey + (key == "" ? "" : $"#{key}");
		}

		internal static string dataKey(string key) => $"{modGuid}#{key}";

		public string? this[string key]
		{
			get => Get<StringItemData>(key)?.Value;
			set => GetOrCreate<StringItemData>(key).Value = value ?? "";
		}

		internal ItemInfo(ItemDrop.ItemData itemData)
		{
			ItemData = itemData;

			string prefix = dataKey("");
			List<string> keys = ItemData.m_customData.Keys.ToList();
			foreach (string key in keys)
			{
				if (key.StartsWith(prefix))
				{
					string unprefixedKey = key.Substring(prefix.Length);
					string[] keyParts = unprefixedKey.Split(new[] { '#' }, 2);
					if (!knownTypes.Contains(keyParts[0]) && Type.GetType(keyParts[0]) is { } type && typeof(CustomItemData).IsAssignableFrom(type))
					{
						addTypeToInheritorsCache(type, keyParts[0]);
					}
				}
			}
		}

		public T GetOrCreate<T>(string key = "") where T : CustomItemData, new() => Add<T>(key) ?? Get<T>(key)!;

		public T? Add<T>(string key = "") where T : CustomItemData, new()
		{
			string compoundKey = classKey(typeof(T), key);
			string fullKey = dataKey(compoundKey);
			if (ItemData.m_customData.ContainsKey(fullKey))
			{
				return null;
			}

			ItemData.m_customData[fullKey] = "";
			T obj = new() { info = selfReference ??= new WeakReference<ItemInfo>(this), Key = key };
			data[compoundKey] = obj;
			obj.Value = ""; // initial Store
			obj.FirstLoad();
			return obj;
		}

		public T? Get<T>(string key = "") where T : class
		{
			if (!typeInheritorsCache.TryGetValue(typeof(T), out HashSet<Type> inheritors))
			{
				if (!typeof(CustomItemData).IsAssignableFrom(typeof(T)) || typeof(T) == typeof(CustomItemData))
				{
					throw new Exception("Trying to get value from ItemDataManager for class not inheriting from " + nameof(ItemData));
				}
				return null;
			}

			foreach (Type inheritor in inheritors)
			{
				string compoundKey = classKey(inheritor, key);
				if (data.TryGetValue(compoundKey, out CustomItemData dataObj))
				{
					return (T?)(object)dataObj;
				}

				string fullKey = dataKey(compoundKey);
				if (ItemData.m_customData.ContainsKey(fullKey))
				{
					return (T?)(object)constructDataObj(compoundKey)!;
				}
			}

			return null;
		}

		public Dictionary<string, T> GetAll<T>() where T : CustomItemData
		{
			LoadAll();
			return data.Values.Where(o => o is T).ToDictionary(o => o.Key, o => (T)o);
		}

		public bool Remove(string key = "") => Remove<StringItemData>(key);

		public bool Remove<T>(string key = "") where T : CustomItemData
		{
			string compoundKey = classKey(typeof(T), key);
			string fullKey = dataKey(compoundKey);
			if (ItemData.m_customData.Remove(fullKey))
			{
				if (data.TryGetValue(compoundKey, out CustomItemData itemData))
				{
					itemData.Unload();
					data.Remove(compoundKey);
				}
				return true;
			}

			return false;
		}

		public bool Remove<T>(T itemData) where T : CustomItemData => Remove<T>(itemData.Key);

		private CustomItemData? constructDataObj(string key)
		{
			string[] keyParts = key.Split(new[] { '#' }, 2);
			if (Type.GetType(keyParts[0]) is not { } type || !typeof(CustomItemData).IsAssignableFrom(type))
			{
				return null;
			}

			CustomItemData obj = (CustomItemData)Activator.CreateInstance(type);
			data[key] = obj;
			obj.info = selfReference ?? new WeakReference<ItemInfo>(this);
			obj.Key = keyParts.Length > 1 ? keyParts[1] : "";
			obj.Load();

			return obj;
		}

		public void Save()
		{
			foreach (CustomItemData itemData in data.Values)
			{
				itemData.Save();
			}
		}

		public void LoadAll()
		{
			string prefix = dataKey("");
			List<string> keys = ItemData.m_customData.Keys.ToList();
			foreach (string key in keys)
			{
				if (key.StartsWith(prefix))
				{
					string unprefixedKey = key.Substring(prefix.Length);
					if (!data.ContainsKey(unprefixedKey))
					{
						constructDataObj(unprefixedKey);
					}
				}
			}
		}

		public IEnumerator<CustomItemData> GetEnumerator()
		{
			LoadAll();
			return data.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private static void SavePrefix(ItemDrop.ItemData __instance)
		{
			SaveItem(__instance);
		}

		private static void SaveInventoryPrefix(Inventory __instance)
		{
			foreach (ItemDrop.ItemData item in __instance.m_inventory)
			{
				SaveItem(item);
			}
		}

		private static void SaveItem(ItemDrop.ItemData item)
		{
			if (ItemExtensions.itemInfo.TryGetValue(item, out ItemInfo info))
			{
				info.Save();
			}
		}

		public Dictionary<string, string>? IsStackableWithOtherInfo(ItemInfo? info)
		{
			LoadAll();
			Dictionary<string, string> newValues = new();
			if (info is not null)
			{
				info.LoadAll();

				HashSet<string> sharedKeys = new(info.data.Keys.Intersect(data.Keys));
				foreach (string key in sharedKeys)
				{
					if (data[key].TryStack(info.data[key]) is not { } newData)
					{
						return null;
					}

					newValues[key] = newData;
				}

				foreach (KeyValuePair<string, CustomItemData> kv in info.data)
				{
					if (!newValues.ContainsKey(kv.Key))
					{
						if (info.data[kv.Key].TryStack(null) is not { } newData)
						{
							return null;
						}

						newValues[kv.Key] = newData;
					}
				}
			}

			foreach (KeyValuePair<string, CustomItemData> kv in data)
			{
				if (!newValues.ContainsKey(kv.Key))
				{
					if (data[kv.Key].TryStack(null) is not { } newData)
					{
						return null;
					}

					newValues[kv.Key] = newData;
				}
			}

			return newValues.ToDictionary(kv => dataKey(kv.Key), kv => kv.Value);
		}

		private static void RegisterForceLoadedTypesAddItem(ItemDrop.ItemData? __result)
		{
			if (__result is not null)
			{
				RegisterForceLoadedTypes(__result);
			}
		}

		private static void RegisterForceLoadedTypes(ItemDrop.ItemData itemData)
		{
			foreach (Type type in ForceLoadTypes)
			{
				string compoundKey = classKey(type, "");
				string fullKey = dataKey(compoundKey);
				if (itemData.m_customData.ContainsKey(fullKey))
				{
					itemData.Data().constructDataObj(compoundKey);
				}
			}
		}

		private static void ItemDropAwake(ItemDrop __instance)
		{
			if (__instance.m_itemData.m_dropPrefab is { } prefab && ItemExtensions.itemInfo.TryGetValue(prefab.GetComponent<ItemDrop>().m_itemData, out ItemInfo info))
			{
				__instance.m_itemData.Data().isCloned = new HashSet<string>(info.data.Values.Select(i => i.CustomDataKey));
			}
		}

		private static void ItemDropAwakeDelayed(ItemDrop __instance)
		{
			if (!ZNetView.m_forceDisableInit)
			{
				RegisterForceLoadedTypes(__instance.m_itemData);
			}
		}

		private static void ItemDataClonePrefix(ItemDrop.ItemData __instance, ItemDrop.ItemData __result) => SaveItem(__instance);

		private static void ItemDataClonePostfix(ItemDrop.ItemData __instance, ItemDrop.ItemData __result)
		{
			if (ItemExtensions.itemInfo.TryGetValue(__instance, out ItemInfo info))
			{
				__result.Data().isCloned = new HashSet<string>(info.data.Values.Select(i => i.CustomDataKey));
			}
		}

		private static void ItemDataClonePostfixDelayed(ItemDrop.ItemData __result)
		{
			RegisterForceLoadedTypes(__result);
		}

		private static void RegisterForceLoadedTypesOnPlayerLoaded(Player __instance)
		{
			foreach (Player.Food food in __instance.m_foods)
			{
				GameObject foodPrefab = ObjectDB.instance.GetItemPrefab(food.m_name);
				if (foodPrefab.GetComponent<ItemDrop>().m_itemData == food.m_item)
				{
					food.m_item = food.m_item.Clone();
					food.m_item.m_dropPrefab = foodPrefab;
				}
				RegisterForceLoadedTypes(food.m_item);
			}
		}

		private static ItemDrop.ItemData? checkingForStackableItemData;

		private static void SaveCheckingForStackableItemData(ItemDrop.ItemData item) => checkingForStackableItemData = item;
		private static void ResetCheckingForStackableItemData() => checkingForStackableItemData = null;

		private static Dictionary<string, string>? newValuesOnStackable;

		private static IEnumerable<CodeInstruction> CheckStackableInFindFreeStackMethods(IEnumerable<CodeInstruction> instructionsEnumerable)
		{
			CodeInstruction[] instructions = instructionsEnumerable.ToArray();
			Label target = (Label)instructions.First(i => i.opcode == OpCodes.Br || i.opcode == OpCodes.Br_S).operand;
			CodeInstruction targetedInstr = instructions.First(i => i.labels.Contains(target));
			CodeInstruction lastBranch = instructions.Reverse().First(i => i.Branches(out Label? label) && targetedInstr.labels.Contains(label!.Value));
			CodeInstruction? loadingInstruction = null;

			for (int i = 0; i < instructions.Length; ++i)
			{
				yield return instructions[i];
				// get hold of the loop variable store (the itemdata we want to compare against)
				if (loadingInstruction == null && instructions[i].opcode == OpCodes.Call && ((MethodInfo)instructions[i].operand).Name == "get_Current")
				{
					loadingInstruction = instructions[i + 1].Clone();
					loadingInstruction.opcode = new Dictionary<OpCode, OpCode>
				{
					{ OpCodes.Stloc_0, OpCodes.Ldloc_0 },
					{ OpCodes.Stloc_1, OpCodes.Ldloc_1 },
					{ OpCodes.Stloc_2, OpCodes.Ldloc_2 },
					{ OpCodes.Stloc_3, OpCodes.Ldloc_3 },
					{ OpCodes.Stloc_S, OpCodes.Ldloc_S }
				}[loadingInstruction.opcode];
				}
				if (instructions[i] == lastBranch)
				{
					yield return loadingInstruction!;
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(ItemInfo), nameof(checkingForStackableItemData)));
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(CheckItemDataIsStackableFindFree)));
					yield return new CodeInstruction(OpCodes.Brfalse, target);
				}
			}
		}

		private static bool CheckItemDataIsStackableFindFree(ItemDrop.ItemData item, ItemDrop.ItemData? target)
		{
			if (target is null)
			{
				return true;
			}

			if (IsStackable(item, target) is { } newValues)
			{
				newValuesOnStackable = newValues;
				return true;
			}

			return false;
		}

		private static void ResetNewValuesOnStackable() => newValuesOnStackable = null;

		private static void ApplyNewValuesOnStackable(ItemDrop.ItemData? __result)
		{
			if (__result is not null && newValuesOnStackable is not null)
			{
				foreach (KeyValuePair<string, string> kv in newValuesOnStackable)
				{
					__result.m_customData[kv.Key] = kv.Value;
				}
			}
		}

		private static Dictionary<string, string>? IsStackable(ItemDrop.ItemData a, ItemDrop.ItemData b)
		{
			if (a.Data() is { } info)
			{
				return info.IsStackableWithOtherInfo(b.Data());
			}

			if (b.Data() is { } otherInfo)
			{
				return otherInfo.IsStackableWithOtherInfo(null);
			}

			return new Dictionary<string, string>();
		}

		private static bool CheckItemDataStackableAddItem(Inventory __instance, ItemDrop.ItemData item, int x, int y, ref Dictionary<string, string>? __state, ref bool __result)
		{
			if (__instance.GetItemAt(x, y) is { } itemAt)
			{
				if (IsStackable(item, itemAt) is not { } newValues)
				{
					__result = false;
					return false;
				}

				__state = newValues;
			}

			return true;
		}

		private static void ApplyCustomItemDataStackableAddItem(Inventory __instance, int x, int y, Dictionary<string, string>? __state, bool __result)
		{
			if (__result && __state is not null)
			{
				foreach (KeyValuePair<string, string> kv in __state)
				{
					__instance.GetItemAt(x, y).m_customData[kv.Key] = kv.Value;
				}
			}
		}

		private static void ApplyCustomItemDataStackableAutoStack(ItemDrop item, Dictionary<string, string> customData)
		{
			item.m_itemData.m_customData = customData;
		}

		private static Dictionary<string, string>? IsStackableItemDrop(ItemDrop drop, ItemDrop.ItemData item) => IsStackable(drop.m_itemData, item);

		private static IEnumerable<CodeInstruction> HandleAutostackableItems(IEnumerable<CodeInstruction> instructionList, ILGenerator ilg)
		{
			// Turn:
			// if (component.m_itemData.m_stack <= num) { ... }
			// into:
			// if (component.m_itemData.m_stack <= num && (dict = IsStackable(this, component)) is not null) { ... ApplyCustomItemDataStackableAutoStack(this, dict); }

			List<CodeInstruction> instructions = instructionList.ToList();
			FieldInfo stack = AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_stack));
			FieldInfo itemData = AccessTools.DeclaredField(typeof(ItemDrop), nameof(ItemDrop.m_itemData));
			for (int i = 0; i < instructions.Count; ++i)
			{
				if (instructions[i].StoresField(stack))
				{
					for (int j = i; j > 0; --j)
					{
						if (instructions[j].Branches(out Label? skipTarget))
						{
							for (int k = j; k > 0; --k)
							{
								if (instructions[k].LoadsField(itemData))
								{
									LocalBuilder dict = ilg.DeclareLocal(typeof(Dictionary<string, string>));
									LocalBuilder itemDataVar = ilg.DeclareLocal(typeof(ItemDrop.ItemData));
									instructions.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ApplyCustomItemDataStackableAutoStack))));
									instructions.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc, dict.LocalIndex));
									instructions.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));

									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Brfalse, skipTarget));
									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Stloc, dict.LocalIndex));
									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Dup, dict.LocalIndex));
									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(IsStackableItemDrop))));
									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Ldloc, itemDataVar.LocalIndex));
									instructions.Insert(j + 1, new CodeInstruction(OpCodes.Ldarg_0));

									instructions.Insert(k + 1, new CodeInstruction(OpCodes.Stloc, itemDataVar.LocalIndex));
									instructions.Insert(k + 1, new CodeInstruction(OpCodes.Dup));

									return instructions;
								}
							}
						}
					}
				}
			}
			throw new Exception("Found no stack store in a branch");
		}

		private static ItemDrop.ItemData? currentlyUpgradingItem;

		private static IEnumerable<CodeInstruction> TransferCustomItemDataOnUpgrade(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			MethodInfo itemDeleter = AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.RemoveItem), new[] { typeof(ItemDrop.ItemData) });
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Callvirt && instruction.OperandIs(itemDeleter))
				{
					yield return new CodeInstruction(OpCodes.Dup);
					yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.DeclaredField(typeof(ItemInfo), nameof(currentlyUpgradingItem)));
				}
				yield return instruction;
			}
		}

		private static void ResetCurrentlyUpgradingItem() => currentlyUpgradingItem = null;

		private static void CopyCustomDataFromUpgradedItem(ItemDrop item)
		{
			if (currentlyUpgradingItem is not null)
			{
				item.m_itemData.m_customData = currentlyUpgradingItem.m_customData;
				if (ItemExtensions.itemInfo.TryGetValue(item.m_itemData, out ItemInfo info))
				{
					info.ItemData = item.m_itemData;

					ItemExtensions.itemInfo.Remove(currentlyUpgradingItem);
					ItemExtensions.itemInfo.Add(item.m_itemData, info);

					foreach (CustomItemData itemData in info.data.Values)
					{
						itemData.Upgraded();
					}
				}
				currentlyUpgradingItem = null;
			}
			else if (item.m_itemData.m_dropPrefab is { } prefab && item.m_itemData.m_customData.Count == 0)
			{
				ZNetView netView = item.GetComponent<ZNetView>();
				ZDO? zdo = netView && netView.IsValid() ? netView.GetZDO() : null;
				if (zdo?.m_ints?.ContainsKey("dataCount".GetStableHashCode()) != true)
				{
					item.m_itemData.m_customData = new Dictionary<string, string>(prefab.GetComponent<ItemDrop>().m_itemData.m_customData);
					if (zdo is not null)
					{
						int num = 0;
						zdo.Set("dataCount", item.m_itemData.m_customData.Count);
						foreach (KeyValuePair<string, string> keyValuePair in item.m_itemData.m_customData)
						{
							zdo.Set($"data_{num}", keyValuePair.Key);
							zdo.Set($"data__{num++}", keyValuePair.Value);
						}
					}
				}
			}
		}

		private static IEnumerable<CodeInstruction> ImportCustomDataOnUpgrade(IEnumerable<CodeInstruction> instructionList)
		{
			List<CodeInstruction> instructions = instructionList.ToList();
			foreach (CodeInstruction instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Stfld && instruction.OperandIs(AccessTools.DeclaredField(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.m_dropPrefab))))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(CopyCustomDataFromUpgradedItem)));
				}
			}
		}

		static ItemInfo()
		{
			Harmony harmony = new("org.bepinex.helpers.ItemDataManager");
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.Save)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(SaveInventoryPrefix)), Priority.First));
			foreach (MethodInfo method in typeof(ItemDrop.ItemData).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == nameof(ItemDrop.SaveToZDO)))
			{
				harmony.Patch(method, prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(SavePrefix)), Priority.First));
			}

			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(CheckItemDataStackableAddItem))), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ApplyCustomItemDataStackableAddItem))));

			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.CanAddItem), new[] { typeof(ItemDrop.ItemData), typeof(int) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(SaveCheckingForStackableItemData))), finalizer: new HarmonyMethod(typeof(ItemInfo), nameof(ResetCheckingForStackableItemData)));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(ItemDrop.ItemData) }), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(SaveCheckingForStackableItemData))), finalizer: new HarmonyMethod(typeof(ItemInfo), nameof(ResetCheckingForStackableItemData)));

			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.FindFreeStackSpace)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(CheckStackableInFindFreeStackMethods))));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.FindFreeStackItem)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(CheckStackableInFindFreeStackMethods))), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ResetNewValuesOnStackable))), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ApplyNewValuesOnStackable))));

			harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.AutoStackItems)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(HandleAutostackableItems))));

			harmony.Patch(AccessTools.DeclaredMethod(typeof(InventoryGui), nameof(InventoryGui.DoCrafting)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(TransferCustomItemDataOnUpgrade))), finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ResetCurrentlyUpgradingItem))));

			// Force loads
			foreach (MethodInfo method in typeof(ItemDrop.ItemData).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.Name == nameof(ItemDrop.LoadFromZDO)))
			{
				harmony.Patch(method, postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(RegisterForceLoadedTypes))));
			}
			// Note: Inventory load implicitly handled by CustomItemData.Clone() handling within AddItem
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Player), nameof(Player.Load)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(RegisterForceLoadedTypesOnPlayerLoaded)), Priority.VeryHigh));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(Inventory), nameof(Inventory.AddItem), new[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string) }), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(RegisterForceLoadedTypesAddItem)), Priority.First));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.Awake)), transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ImportCustomDataOnUpgrade)), Priority.First), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ItemDropAwake)), Priority.First));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.Awake)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ItemDropAwakeDelayed)), Priority.First - 1));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone)), prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ItemDataClonePrefix))), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ItemDataClonePostfix)), Priority.HigherThanNormal));
			harmony.Patch(AccessTools.DeclaredMethod(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.Clone)), postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(ItemInfo), nameof(ItemDataClonePostfixDelayed)), Priority.HigherThanNormal - 1));
		}
	}

	[PublicAPI]
	public class ForeignItemInfo : IEnumerable<object>
	{
		public string Mod => (string?)foreignItemInfo.GetType().GetProperty(nameof(Mod))?.GetValue(foreignItemInfo) ?? "";
		public ItemDrop.ItemData ItemData { get; private set; }

		private readonly object foreignItemInfo;

		public string? this[string key]
		{
			get
			{
				if (foreignItemInfo.GetType().InvokeMember("Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, foreignItemInfo, new object[] { key }) is { } stringData)
				{
					return (string?)stringData.GetType().GetProperty("Value")?.GetValue(stringData);
				}

				return null;
			}
			set
			{
				foreignItemInfo.GetType().GetMethod("set_Item", BindingFlags.Public | BindingFlags.Instance)?.Invoke(foreignItemInfo, new object?[] { key, value });
			}
		}

		internal ForeignItemInfo(ItemDrop.ItemData itemData, object foreignItemInfo)
		{
			ItemData = itemData;
			this.foreignItemInfo = foreignItemInfo;
		}

		public T GetOrCreate<T>(string key = "") where T : class, new() => Add<T>(key) ?? Get<T>(key)!;

		private object? call(string name, object?[] values, Type?[] args, Type? generic = null)
		{
			foreach (MethodInfo method in foreignItemInfo.GetType().GetMethods())
			{
				if (method.Name == name && method.GetParameters().Select(p => p.ParameterType.IsGenericParameter ? null : p.ParameterType).SequenceEqual(args) && generic is not null == method.IsGenericMethod)
				{
					MethodInfo call = method;
					if (generic is not null)
					{
						call = call.MakeGenericMethod(generic);
					}

					call.Invoke(foreignItemInfo, values);
				}
			}
			return null;
		}

		public T? Add<T>(string key = "") where T : class, new() => call(nameof(Add), new object[] { key }, new[] { typeof(string) }, typeof(T)) as T;

		public T? Get<T>(string key = "") where T : class => call(nameof(Get), new object[] { key }, new[] { typeof(string) }, typeof(T)) as T;

		public Dictionary<string, T> GetAll<T>() where T : class => call(nameof(GetAll), Array.Empty<object?>(), Array.Empty<Type?>(), typeof(T)) as T as Dictionary<string, T> ?? new Dictionary<string, T>();

		public bool Remove(string key = "") => call(nameof(Add), new object[] { key }, new[] { typeof(string) }) as bool? ?? false;

		public bool Remove<T>(string key = "") where T : class => call(nameof(Remove), new object[] { key }, new[] { typeof(string) }, typeof(T)) as bool? ?? false;

		public bool Remove<T>(T itemData) where T : class => call(nameof(Remove), new object[] { itemData }, new Type?[] { null }, typeof(T)) as bool? ?? false;

		public void Save() => call(nameof(Save), Array.Empty<object?>(), Array.Empty<Type?>());
		public void LoadAll() => call(nameof(LoadAll), Array.Empty<object?>(), Array.Empty<Type?>());

		public IEnumerator<object> GetEnumerator() => call(nameof(GetEnumerator), Array.Empty<object?>(), Array.Empty<Type?>()) as IEnumerator<object> ?? new List<object>().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	[PublicAPI]
	public static class ItemExtensions
	{
		internal static readonly ConditionalWeakTable<ItemDrop.ItemData, ItemInfo> itemInfo = new();
		private static readonly ConditionalWeakTable<ItemDrop.ItemData, Dictionary<string, ForeignItemInfo?>> foreignItemInfo = new();

		public static ItemInfo Data(this ItemDrop.ItemData item)
		{
			if (itemInfo.TryGetValue(item, out ItemInfo info))
			{
				return info;
			}
			itemInfo.Add(item, info = new ItemInfo(item));
			return info;
		}

		public static ForeignItemInfo? Data(this ItemDrop.ItemData item, string mod)
		{
			Dictionary<string, ForeignItemInfo?> foreignInfos = foreignItemInfo.GetOrCreateValue(item);
			if (foreignInfos.TryGetValue(mod, out ForeignItemInfo? modObject))
			{
				return modObject;
			}
			if (!Chainloader.PluginInfos.TryGetValue(mod, out PluginInfo plugin))
			{
				return null;
			}

			if (plugin.Instance.GetType().Assembly.GetType(typeof(ItemExtensions).FullName)?.GetMethod(nameof(Data), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(ItemDrop.ItemData) }, Array.Empty<ParameterModifier>())?.Invoke(null, new object[] { item }) is { } foreignItemData)
			{
				return foreignInfos[mod] = new ForeignItemInfo(item, foreignItemData);
			}

			Debug.LogWarning($"Mod {mod} has an {typeof(ItemExtensions).FullName} class, but no Data(ItemDrop.ItemData) method could be called on it.");
			return foreignInfos[mod] = null;
		}
	}
}