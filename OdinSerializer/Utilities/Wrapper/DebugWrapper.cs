namespace OdinSerializer.Utilities.Wrapper;

#if UNITY
using UnityEngine;
#elif GODOT
using Godot;
#endif

public static class DebugWrapper
{
    public static void Log(string message)
    {
        Log(message, null);
    }

    public static void LogError(string message)
    {
        LogError(message, null);
    }

    public static void LogWarning(string message)
    {
        LogWarning(message, null);
    }

    public static void LogException(Exception error)
    {
        LogException(error, null);
    }

    public static void Log(object message, object? context = null)
    {
#if UNITY
        Debug.Log(message is string mStr ? RichText.ConvertGodotToUnity(mStr) : message, context as Object);
#elif GODOT
        GD.PrintRich(message is string mStr ? RichText.ConvertUnityToGodot(mStr) : message);
#endif
    }

    public static void LogError(object message, object? context = null)
    {
#if UNITY
        Debug.LogError(message, context as Object);
#elif GODOT
        GD.PrintErr(message);
#endif
    }

    public static void LogWarning(object message, object? context = null)
    {
#if UNITY
        Debug.LogWarning(message, context as Object);
#elif GODOT
        GD.PushWarning(message);
#endif
    }

    public static void LogException(Exception error, object? context = null)
    {
#if UNITY
        Debug.LogException(error, context as Object);
#elif GODOT
        GD.PushError(error);
#endif
    }

}