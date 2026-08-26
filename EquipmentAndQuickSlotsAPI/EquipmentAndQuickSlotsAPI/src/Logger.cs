using UnityEngine;

namespace EquipmentAndQuickSlotsAPI
{
    /// <summary>
    /// Minimal logger for the shim: warnings and errors always go to the Unity log (a broken
    /// integration must be visible), debug output only in debug builds of the consumer.
    /// </summary>
    public class Logger
    {
        private const string Prefix = "[EquipmentAndQuickSlotsAPI] ";

        public void LogDebug(string message)
        {
#if DEBUG
            Debug.Log(Prefix + message);
#endif
        }

        public void LogInfo(string message) => Debug.Log(Prefix + message);

        public void LogWarning(string message) => Debug.LogWarning(Prefix + message);

        public void LogError(string message) => Debug.LogError(Prefix + message);
    }
}
