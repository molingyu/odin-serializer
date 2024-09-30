namespace OdinSerializer.Utilities.Wrapper;

#if UNITY
using UnityEngine;
#elif GODOT
using Godot;
using System.Reflection;
#endif

public static class DebugWrapper
{
#if GODOT
    /// <summary>
    /// Whether the Godot native runtime is available in the current process. When false
    /// (for example in unit tests or standalone tools), any call into GodotSharp's GD API
    /// would crash the process with an access violation, so logging falls back to the console.
    /// </summary>
    private static readonly bool GodotRuntimeAvailable = DetectGodotRuntime();

    private static bool DetectGodotRuntime()
    {
        try
        {
            // GodotSharp's NativeFuncs is only initialized by the Godot host process;
            // its 'initialized' flag stays false when GodotSharp is used standalone.
            var nativeFuncs = typeof(GD).Assembly.GetType("Godot.NativeInterop.NativeFuncs");

            if (nativeFuncs == null)
            {
                return false;
            }

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var property = nativeFuncs.GetProperty("Initialized", flags);

            if (property != null)
            {
                return property.GetValue(null) is true;
            }

            var field = nativeFuncs.GetField("initialized", flags) ?? nativeFuncs.GetField("_initialized", flags);

            return field != null && field.GetValue(null) is true;
        }
        catch
        {
            return false;
        }
    }
#endif

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
        if (GodotRuntimeAvailable)
        {
            GD.PrintRich(message is string mStr ? RichText.ConvertUnityToGodot(mStr) : message);
        }
        else
        {
            Console.WriteLine(message);
        }
#endif
    }

    public static void LogError(object message, object? context = null)
    {
#if UNITY
        Debug.LogError(message, context as Object);
#elif GODOT
        if (GodotRuntimeAvailable)
        {
            GD.PrintErr(message);
        }
        else
        {
            Console.Error.WriteLine(message);
        }
#endif
    }

    public static void LogWarning(object message, object? context = null)
    {
#if UNITY
        Debug.LogWarning(message, context as Object);
#elif GODOT
        if (GodotRuntimeAvailable)
        {
            GD.PushWarning(message);
        }
        else
        {
            Console.WriteLine("WARNING: " + message);
        }
#endif
    }

    public static void LogException(Exception error, object? context = null)
    {
#if UNITY
        Debug.LogException(error, context as Object);
#elif GODOT
        if (GodotRuntimeAvailable)
        {
            GD.PushError(error);
        }
        else
        {
            Console.Error.WriteLine(error);
        }
#endif
    }

}
