using System;

namespace ThunderKit.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HideFromScriptWindow : Attribute
    {
    }
}