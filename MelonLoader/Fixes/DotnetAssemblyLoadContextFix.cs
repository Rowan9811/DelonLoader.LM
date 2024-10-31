#if NET6_0_OR_GREATER
using HarmonyLib;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace MelonLoader.Fixes
{
    internal class DotnetAssemblyLoadContextFix
    {
        private delegate Assembly DelegateInternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols);

        private static readonly Dictionary<string, Assembly> s_loadfile = new Dictionary<string, Assembly>();

        private static readonly MethodInfo AlcInternalLoad = typeof(AssemblyLoadContext).GetMethod("InternalLoad", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo AlcQCallLoadFromPath = typeof(AssemblyLoadContext).GetMethod("LoadFromPath", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo AlcQCallLoadFromStream = typeof(AssemblyLoadContext).GetMethod("LoadFromStream", BindingFlags.NonPublic | BindingFlags.Static);

        private static DelegateInternalLoad DefaultContextInternalLoad = AlcInternalLoad.CreateDelegate<DelegateInternalLoad>(AssemblyLoadContext.Default);


        internal static void Install()
        {
            try
            {
                Core.HarmonyInstance.Patch(AccessTools.Method(typeof(Assembly), nameof(Assembly.Load), new Type[] { typeof(byte[]), typeof(byte[]) }), new HarmonyMethod(typeof(DotnetAssemblyLoadContextFix), nameof(PreAssemblyLoad)));
                Core.HarmonyInstance.Patch(AccessTools.Method(typeof(Assembly), nameof(Assembly.LoadFile)), new HarmonyMethod(typeof(DotnetAssemblyLoadContextFix), nameof(PreAssemblyLoadFile)));

                //We have to load everything required for the verifier to avoid getting stuck in an infinite loop, prior to hooking AssemblyLoadContext.
                AssemblyVerifier.EnsureInitialized();
            }
            catch (Exception ex) { MelonLogger.Warning($"DotnetAssemblyLoadContextFix Exception: {ex}"); }
        }

        public static bool PreAssemblyLoad(byte[] rawAssembly, byte[] rawSymbolStore, ref Assembly __result)
        {
            //if(MelonDebug.IsEnabled() && !Environment.StackTrace.Contains("HarmonyLib"))
            //    MelonDebug.Msg($"[.NET AssemblyLoadContext Fix] Redirecting Assembly.Load call with {rawAssembly.Length}-byte assembly to AssemblyLoadContext.Default. Mod Devs: You may wish to use this explictly.");

            __result = DefaultContextInternalLoad(rawAssembly, rawSymbolStore);

            //Prevent loading in non-default context, which is default behaviour of Assembly.Load
            return false;
        }

        public static bool PreAssemblyLoadFile(string path, ref Assembly __result)
        {
            //MelonDebug.Msg($"[.NET AssemblyLoadContext Fix] Redirecting Assembly.LoadFile({path}) call to AssemblyLoadContext.Default.LoadFromAssemblyPath. Mod Devs: You may wish to use this explictly.");

            string normalizedPath = Path.GetFullPath(path);

            lock (s_loadfile)
            {
                if (s_loadfile.TryGetValue(normalizedPath, out __result))
                    return false;

                __result = AssemblyLoadContext.Default.LoadFromAssemblyPath(normalizedPath);

                s_loadfile.Add(normalizedPath, __result);
            }

            return false;
        }

        public static bool PreAlcLoadFromPath(string ilPath)
        {
            return true;
        }

        public static unsafe bool PreAlcLoadFromStream(IntPtr ptrAssemblyArray, int iAssemblyArrayLen)
        {
            //MelonDebug.Msg($"[ALC FromStream] Validating {iAssemblyArrayLen}-byte assembly...");

            byte[] assemblyBytes = new byte[iAssemblyArrayLen];
            Marshal.Copy(ptrAssemblyArray, assemblyBytes, 0, iAssemblyArrayLen);

            //And once again, continue to run the runtime QCall.
            return true;
        }

    }
}
#endif