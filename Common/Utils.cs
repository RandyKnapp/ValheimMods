using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;
using Object = System.Object;

namespace Common
{
    public static class Utils
    {
        public static void PrintObject(object o)
        {
            if (o == null)
            {
                Debug.Log("null");
            }
            else
            {
                Debug.Log(o + ":\n" + GetObjectString(o, "  "));
            }
        }

        public static string GetObjectString(object obj, string indent)
        {
            var output = "";
            Type type = obj.GetType();
            var publicFields = type.GetFields().Where(f => f.IsPublic);
            foreach (var f in publicFields)
            {
                var value = f.GetValue(obj);
                var valueString = value == null ? "null" : value.ToString();
                output += $"\n{indent}{f.Name}: {valueString}";
            }

            return output;
        }

        public static Sprite LoadSpriteFromFile(string spritePath)
        {
            spritePath = Path.Combine(Paths.PluginPath, spritePath);
            if (File.Exists(spritePath))
            {
                byte[] fileData = File.ReadAllBytes(spritePath);
                Texture2D tex = new Texture2D(20, 20);
                if (tex.LoadImage(fileData))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(), 100);
                }
            }

            return null;
        }

        public static Sprite LoadSpriteFromFile(string modFolder, string iconName)
        {
            var spritePath = Path.Combine(modFolder, iconName);
            return LoadSpriteFromFile(spritePath);
        }

        public static string RemoveBetween(string s, string from, string to)
        {
            int start = 0;
            while (start >= 0)
            {
                start = s.IndexOf(from, StringComparison.InvariantCulture);
                if (start < 0)
                {
                    break;
                }

                int end = s.IndexOf(to, start, StringComparison.InvariantCulture);
                if (end < 0)
                {
                    break;
                }

                s = s.Remove(start, end - start + to.Length);
            }

            return s;
        }

        public static void CopyFields(object originalObject, object cloneObject, Type typeToReflect, BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public)
        {
            foreach (FieldInfo fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                fieldInfo.SetValue(cloneObject, originalFieldValue);
            }
        }
    }

    public static class ArrayUtils
    {
        public static bool IsNullOrEmpty<T>(T[] a)
        {
            return a == null || a.Length == 0;
        }
    }

    public static class ListExtensions
    {
        public static bool TryFind<T>(this List<T> list, Predicate<T> predicate, out T result)
        {
            var index = list.FindIndex(predicate);
            if (index != -1)
            {
                result = list[index];
                return true;
            }
            result = default(T);
            return false;
        }
    }
}
