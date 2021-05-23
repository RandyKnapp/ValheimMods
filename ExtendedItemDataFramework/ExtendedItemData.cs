using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    public delegate void NewExtendedItemDataHandler(ExtendedItemData itemData);

    public abstract class BaseExtendedItemComponent
    {
        public string TypeName;
        public ExtendedItemData ItemData;

        protected BaseExtendedItemComponent(string typeName, ExtendedItemData parent)
        {
            TypeName = typeName;
            ItemData = parent;
        }

        public void Save()
        {
            ItemData?.Save();
        }

        public abstract string Serialize();
        public abstract void Deserialize(string data);
        public abstract BaseExtendedItemComponent Clone();
    }

    public class ExtendedItemData : ItemDrop.ItemData
    {
        public const string StartDelimiter = "<|";
        public const string EndDelimiter = "|>";
        public const string StartDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.startdelimiter";
        public const string EndDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.enddelimiter";

        public static event NewExtendedItemDataHandler NewExtendedItemData;
        public static event NewExtendedItemDataHandler LoadExtendedItemData;

        public readonly List<BaseExtendedItemComponent> Components = new List<BaseExtendedItemComponent>();
        private readonly StringBuilder _sb = new StringBuilder();

        // New item
        private ExtendedItemData()
        {
        }

        private void Initialize()
        {
            Components.Add(new CrafterNameData(this));
            NewExtendedItemData?.Invoke(this);
            Save();
        }

        // Convert item
        public ExtendedItemData(ItemDrop.ItemData from)
        {
            m_stack = from.m_stack;
            m_durability = from.m_durability;
            m_quality = from.m_quality;
            m_variant = from.m_variant;
            m_shared = from.m_shared;
            m_crafterID = from.m_crafterID;
            m_crafterName = from.m_crafterName;
            m_gridPos = from.m_gridPos;
            m_equiped = from.m_equiped;
            m_dropPrefab = from.m_dropPrefab;
            m_lastAttackTime = from.m_lastAttackTime;
            m_lastProjectile = from.m_lastProjectile;

            if (from is ExtendedItemData fromExtendedItemData)
            {
                ExtendedItemDataFramework.LogWarning($"Copying ExtendedItemData ({from.m_shared.m_name})");
                Components.AddRange(fromExtendedItemData.Components);
            }
            else
            {
                ExtendedItemDataFramework.LogWarning($"Converting old ItemData to new ExtendedItemData ({from.m_shared.m_name})");
                var crafterNameData = new CrafterNameData(this) { CrafterName = from.m_crafterName };
                Components.Add(crafterNameData);
                NewExtendedItemData?.Invoke(this);
            }
            
            Save();
        }

        // Load item from save data
        public ExtendedItemData(ItemDrop.ItemData prefab, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string extendedData)
        {
            ExtendedItemDataFramework.LogWarning($"Loading ExtendedItemData from data ({prefab.m_shared.m_name}), data: {extendedData}");
            m_stack = Mathf.Min(stack, prefab.m_shared.m_maxStackSize);
            m_durability = durability;
            m_quality = quality;
            m_variant = variant;
            m_shared = prefab.m_shared;
            m_crafterID = crafterID;
            m_crafterName = extendedData;
            m_gridPos = pos;
            m_equiped = equiped;
            m_dropPrefab = prefab.m_dropPrefab;
            m_lastAttackTime = prefab.m_lastAttackTime;
            m_lastProjectile = prefab.m_lastProjectile;

            Load();
            LoadExtendedItemData?.Invoke(this);
        }

        public T GetComponent<T>() where T : BaseExtendedItemComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        public T AddComponent<T>(/*params object[] constructorArgs*/) where T : BaseExtendedItemComponent
        {
            if (GetComponent<T>() == null)
            {
                //var args = (new object[] {this}).Concat(constructorArgs).ToArray();
                //var newComponent = Activator.CreateInstance(typeof(T), args) as T;
                var newComponent = Activator.CreateInstance(typeof(T), this) as T;
                Components.Add(newComponent);
                return newComponent;
            }

            ExtendedItemDataFramework.LogWarning($"Tried to add component ({typeof(T)}) to item ({m_shared.m_name}) but a component of that type already exists!");
            return null;
        }

        public void RemoveComponent<T>() where T : BaseExtendedItemComponent
        {
            var foundComponent = GetComponent<T>();
            if (foundComponent != null)
            {
                Components.Remove(foundComponent);
            }
        }

        public T ReplaceComponent<T>() where T : BaseExtendedItemComponent
        {
            RemoveComponent<T>();
            return AddComponent<T>();
        }

        public void Save()
        {
            if (!ExtendedItemDataFramework.Enabled)
            {
                var crafterNameComponent = GetComponent<CrafterNameData>();
                m_crafterName = crafterNameComponent?.CrafterName ?? string.Empty;
                ExtendedItemDataFramework.Log($"Reverted data to non-extended mode for item ({m_shared.m_name}). m_crafterName: ({m_crafterName})");
                return;
            }

            _sb.Clear();
            foreach (var component in Components)
            {
                _sb.Append($"{StartDelimiter}{EscapeDataText(component.TypeName)}{EndDelimiter}");

                var serializedComponent = component.Serialize();
                _sb.Append(EscapeDataText(serializedComponent));
            }

            m_crafterName = _sb.ToString();
            if (m_equiped)
            {
                UpdatePlayerZDOForEquipment();
            }

            ExtendedItemDataFramework.LogWarning($"Saved Extended Item ({m_shared.m_name}). Data: {m_crafterName}");
        }

        public void Load()
        {
            if (m_crafterName.StartsWith(StartDelimiter))
            {
                Components.Clear();
                var serializedComponents = m_crafterName.Split(new []{ StartDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                ExtendedItemDataFramework.Log($"(Found component save data: {serializedComponents.Length})");
                foreach (var component in serializedComponents)
                {
                    var parts = component.Split(new[] { EndDelimiter }, StringSplitOptions.None);
                    var typeString = RestoreDataText(parts[0]);
                    var data = parts.Length == 2 ? parts[1] : string.Empty;
                    ExtendedItemDataFramework.Log($"  Component: type: {typeString}, data: {data}");

                    var type = Type.GetType(typeString);
                    if (type == null)
                    {
                        ExtendedItemDataFramework.LogError($"Could not deserialize ExtendedItemComponent type ({typeString})");
                        continue;
                    }

                    var newComponent = Activator.CreateInstance(type, this) as BaseExtendedItemComponent;
                    if (newComponent == null)
                    {
                        ExtendedItemDataFramework.LogError($"Could not instantiate extended item component type ({type}) while loading object ({m_shared.m_name})");
                        continue;
                    }
                    newComponent.Deserialize(RestoreDataText(data));
                    Components.Add(newComponent);
                }
            }
            else
            {
                ExtendedItemDataFramework.Log($"Loaded item from data, but it was not extended. Initializing now ({m_shared.m_name})");
                Initialize();
            }
        }

        public static readonly Dictionary<ItemType, string> ZDOIdentifierMap = new Dictionary<ItemType, string>
        {
            { ItemType.Chest, "ChestItem ExtendedItemData" },
            { ItemType.Legs, "LegItem ExtendedItemData" },
            { ItemType.Helmet, "HelmetItem ExtendedItemData" },
            { ItemType.Shoulder, "ShoulderItem ExtendedItemData" },
            { ItemType.Utility, "UtilityItem ExtendedItemData" },
        };

        public void UpdatePlayerZDOForEquipment(bool equip = true)
        {
            if (!(Player.m_localPlayer is Player player) || !(player.m_nview?.GetZDO() is ZDO zdo))
            {
                return;
            }

            string data = equip ? m_crafterName : "";

            if (m_shared.m_itemType == ItemType.Bow || m_shared.m_itemType == ItemType.OneHandedWeapon || m_shared.m_itemType == ItemType.TwoHandedWeapon || m_shared.m_itemType == ItemType.Shield)
            {
                if (player.m_leftItem?.m_dropPrefab.name == m_dropPrefab.name)
                {
                    Debug.LogWarning($"Set my extended equipment in zdo {m_shared.m_itemType}");
                    zdo.Set("LeftItem ExtendedItemData", data);
                }
                if (player.m_rightItem?.m_dropPrefab.name == m_dropPrefab.name)
                {
                    Debug.LogWarning($"Set my extended equipment in zdo {m_shared.m_itemType}");
                    zdo.Set("RightItem ExtendedItemData", data);
                }
            }
            else if (ZDOIdentifierMap.TryGetValue(m_shared.m_itemType, out string zdoKey))
            {
                Debug.LogWarning($"Set my extended equipment in zdo {m_shared.m_itemType}");
                zdo.Set(zdoKey, data);
            }
        }

        public string EscapeDataText(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Replace(StartDelimiter, StartDelimiterEscaped).Replace(EndDelimiter, EndDelimiterEscaped);
        }

        public string RestoreDataText(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Replace(StartDelimiterEscaped, StartDelimiter).Replace(EndDelimiterEscaped, EndDelimiter);
        }

        public ExtendedItemData ExtendedClone()
        {
            var result = new ExtendedItemData();
            result.m_stack = m_stack;
            result.m_durability = m_durability;
            result.m_quality = m_quality;
            result.m_variant = m_variant;
            result.m_shared = m_shared;
            result.m_crafterID = m_crafterID;
            result.m_crafterName = m_crafterName;
            result.m_gridPos = m_gridPos;
            result.m_equiped = m_equiped;
            result.m_dropPrefab = m_dropPrefab;
            result.m_lastAttackTime = m_lastAttackTime;
            result.m_lastProjectile = m_lastProjectile;

            foreach (var component in Components)
            {
                result.Components.Add(component.Clone());
            }

            return result;
        }
    }
}
