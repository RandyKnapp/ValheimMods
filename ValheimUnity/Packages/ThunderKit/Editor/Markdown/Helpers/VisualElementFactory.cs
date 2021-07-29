using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Markdown.Helpers
{
    public static class VisualElementFactory 
    {
        public static T GetTextElement<T>(string text, string className) where T : TextElement, new()
        {
            T element = GetClassedElement<T>(className);

            element.text = text;
            element.pickingMode = PickingMode.Ignore;

            return element;
        }

        public static T GetTextElement<T>(string text, params string[] classNames) where T : TextElement, new()
        {
            T element = GetClassedElement<T>(classNames);

            element.text = text;
            element.pickingMode = PickingMode.Ignore;

            return element;
        }

        public static T GetClassedElement<T>(string className) where T : VisualElement, new()
        {
            T element = new T
            {
                name = className
            };
            element.AddToClassList(className);

            return element;
        }

        public static T GetClassedElement<T>(params string[] classNames) where T : VisualElement, new()
        {
            T element = new T();

            if (classNames == null || classNames.Length == 0) return element;
            element.name = classNames[0];

            for (int i = 0; i < classNames.Length; i++)
                element.AddToClassList(classNames[i]);

            return element;
        }
    }
}