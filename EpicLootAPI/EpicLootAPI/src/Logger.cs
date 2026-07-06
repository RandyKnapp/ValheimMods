using System;

namespace EpicLootAPI;

public class Logger
{
    public event Action<string> OnError;
    public event Action<string> OnDebug;
    public event Action<string> OnWarning;
    
    public void LogError(string message) => OnError?.Invoke(message);
    public void LogDebug(string message) => OnDebug?.Invoke(message);
    public void LogWarning(string message) => OnWarning?.Invoke(message);
}