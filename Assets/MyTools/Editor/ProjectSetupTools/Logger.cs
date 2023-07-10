using System;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Logger
{
    public static string Color(this string myStr, string color)
    {
        return $"<color={color}>{myStr}</color>";
    }

    private static void DoLog(Action<string, Object> LogFunction, string prefix, Object myObj, params object[] msg)
    {
#if UNITY_EDITOR
        var name = (myObj ? myObj.name : "NullObject");
        LogFunction($"{prefix}[{name}]: {String.Join("; ", msg)}\n ", myObj);
#endif
    }

    public static void Log(this Object myObj, params object[] msg)
    {
        DoLog(Debug.Log, "", myObj, msg);
    }

    public static void LogDetail(this Object myObj, params object[] msg)
    {
        DoLog(Debug.Log, "", myObj, msg);
    }

    public static void LogError(this Object myObj, params object[] msg)
    {
        DoLog(Debug.LogError, "", myObj, msg);
    }

    public static void LogWarning(this Object myObj, params object[] msg)
    {
        DoLog(Debug.LogWarning,"", myObj, msg);
    }

    public static void LogSuccess(this Object myObj, params object[] msg)
    {
        DoLog(Debug.Log, "", myObj, msg);
    }
}