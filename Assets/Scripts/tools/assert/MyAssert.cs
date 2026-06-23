using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;

namespace tools.assert
{
    public static class MyAssert
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message = "", 
            [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            if (condition)
            {
                return;
            }
            string fullMessage = $"Assert failed: {message}\n File: {file}\n Line: {line}\n Member: {member}";
            Debug.LogError(fullMessage);
            throw new Exception(fullMessage);
        }
    }
}