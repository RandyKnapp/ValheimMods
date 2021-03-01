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

        public abstract string Serialize();
        public abstract void Deserialize(string data);
        public abstract BaseExtendedItemComponent Clone();
    }

    public class ExtendedItemData : ItemDrop.ItemData
    {
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
                Debug.LogWarning($"Copying ExtendedItemData ({from.m_shared.m_name})");
                Components.AddRange(fromExtendedItemData.Components);
            }
            else
            {
                Debug.LogWarning($"Converting old ItemData to new ExtendedItemData ({from.m_shared.m_name})");
                var crafterNameData = new CrafterNameData(this) { CrafterName = from.m_crafterName };
                Components.Add(crafterNameData);
            }

            NewExtendedItemData?.Invoke(this);
            Save();
        }

        // Load item from save data
        public ExtendedItemData(ItemDrop.ItemData prefab, int stack, float durability, Vector2i pos, bool equiped, int quality, int variant, long crafterID, string extendedData)
        {
            Debug.LogWarning($"Loading ExtendedItemData from data ({prefab.m_shared.m_name}), data: {extendedData}");
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
            foreach (var component in Components)
            {
                var componentT = component as T;
                if (componentT != null)
                {
                    return componentT;
                }
            }

            return null;
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

            Debug.LogWarning($"Tried to add component ({typeof(T)}) to item ({m_shared.m_name}) but a component of that type already exists!");
            return null;
        }

        public void Save()
        {
            _sb.Clear();
            foreach (var component in Components)
            {
                var serializedComponent = component.Serialize();
                _sb.Append($"<|{component.TypeName}|>");
                _sb.Append(serializedComponent);
            }

            m_crafterName = _sb.ToString();

            Debug.LogWarning($"Saved Extended Item ({m_shared.m_name}). Data: {m_crafterName}");
        }

        public void Load()
        {
            if (m_crafterName.StartsWith("<|"))
            {
                Components.Clear();
                var serializedComponents = m_crafterName.Split(new []{ "<|" }, StringSplitOptions.RemoveEmptyEntries);
                Debug.Log($"(Found component save data: {serializedComponents.Length})");
                foreach (var component in serializedComponents)
                {
                    var parts = component.Split(new[] { "|>" }, StringSplitOptions.None);
                    var typeString = parts[0];
                    var data = parts.Length == 2 ? parts[1] : string.Empty;
                    Debug.Log($"  Component: type: {typeString}, data: {data}");

                    var type = Type.GetType(typeString);
                    if (type == null)
                    {
                        Debug.LogError($"Could not deserialize ExtendedItemComponent type ({parts[0]})");
                        continue;
                    }

                    var newComponent = Activator.CreateInstance(type, this) as BaseExtendedItemComponent;
                    if (newComponent == null)
                    {
                        Debug.LogError($"Could not instantiate extended item component type ({type}) while loading object ({m_shared.m_name})");
                        continue;
                    }
                    newComponent.Deserialize(data);
                    Components.Add(newComponent);
                }
            }
            else
            {
                Debug.Log($"Loaded item from data, but it was not extended. Initializing now ({m_shared.m_name})");
                Initialize();
            }
        }

        public BaseExtendedItemComponent DeserializeComponent(Type type, string json)
        {
            // TODO
            return null;
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
