using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MelonLoader.Utils
{
    internal static class Assertion
    {
        internal static bool ShouldContinue = true;

        internal static void ThrowInternalFailure(string msg)
        {
            if (!ShouldContinue)
                return;

            ShouldContinue = false;

            var timestamp = LoggerUtils.GetTimeStamp();
            MelonLogger.WriteLogToFile($"[{timestamp}] [INTERNAL FAILURE] {msg}");
            Environment.Exit(1);
        }
    }
}