using System;
using System.Diagnostics;
using MelonLoader.InternalUtils;
using MelonLoader.MonoInternals;
using bHapticsLib;
#pragma warning disable IDE0051 // Prevent the IDE from complaining about private unreferenced methods

namespace MelonLoader
{
	internal static class Core
    {
        internal static HarmonyLib.Harmony HarmonyInstance;

        private static int Initialize()
        {
            AppDomain curDomain = AppDomain.CurrentDomain;
            Fixes.UnhandledException.Install(curDomain);
            Fixes.ServerCertificateValidation.Install();

            MelonUtils.Setup(curDomain);
            Assertions.LemonAssertMapping.Setup();


            JNISharp.NativeInterface.JNI.Initialize(new JNISharp.NativeInterface.JavaVMInitArgs());

            // TODO: MonoLibrary stuff
#if !__ANDROID__
            if (!MonoLibrary.Setup()
                || !MonoResolveManager.Setup())
                return 1;
#else
            foreach (var file in System.IO.Directory.GetFiles(MelonUtils.UserLibsDirectory, "*.dll"))
            {
                try
                {
                    System.Reflection.Assembly.LoadFrom(file);
                    MelonDebug.Msg("Loaded " + System.IO.Path.GetFileName(file) + " from UserLibs!");
                }
                catch (Exception e)
                {
                    MelonLogger.Msg("Failed to load " + System.IO.Path.GetFileName(file) + " from UserLibs!");
                    MelonLogger.Error(e.ToString());
                }
            }
#endif

            HarmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);

            Fixes.ForcedCultureInfo.Install();
            Fixes.InstancePatchFix.Install();
            Fixes.ProcessFix.Install();
#if __ANDROID__
            Fixes.DateTimeOverride.Install();
            Fixes.NetSocketsFix.Install();
#endif
#if !__ANDROID__
            PatchShield.Install();
#endif

            MelonPreferences.Load();

            MelonCompatibilityLayer.LoadModules();

            bHapticsManager.Connect(BuildInfo.Name, UnityInformationHandler.GameName);

            MelonHandler.LoadMelonsFromDirectory<MelonPlugin>(MelonHandler.PluginsDirectory);
            MelonEvents.MelonHarmonyEarlyInit.Invoke();
            MelonEvents.OnPreInitialization.Invoke();

            return 0;
        }

        private static int PreStart()
        {
            MelonEvents.OnApplicationEarlyStart.Invoke();
#if !__ANDROID__
            return MelonStartScreen.LoadAndRun(Il2CppGameSetup);
#else
            return Il2CppGameSetup();
#endif
        }

        private static int Il2CppGameSetup()
            => Il2CppAssemblyGenerator.Run() ? 0 : 1;

        private static int Start()
        {
            MelonEvents.OnPreModsLoaded.Invoke();
            MelonHandler.LoadMelonsFromDirectory<MelonMod>(MelonHandler.ModsDirectory);

            MelonEvents.OnPreSupportModule.Invoke();
            if (!SupportModule.Setup())
                return 1;

            AddUnityDebugLog();
            RegisterTypeInIl2Cpp.SetReady();

            MelonEvents.MelonHarmonyInit.Invoke();
            MelonEvents.OnApplicationStart.Invoke();

            return 0;
        }

        internal static void Quit()
        {
            MelonPreferences.Save();

            HarmonyInstance.UnpatchSelf();
            bHapticsManager.Disconnect();

            MelonLogger.Flush();

            if (MelonLaunchOptions.Core.QuitFix)
                Process.GetCurrentProcess().Kill();
        }

        private static void AddUnityDebugLog()
        {
            var msg = "~   This Game has been MODIFIED using MelonLoader. DO NOT report any issues to the Game Developers!   ~";
            var line = new string('-', msg.Length);
            SupportModule.Interface.UnityDebugLog(line);
            SupportModule.Interface.UnityDebugLog(msg);
            SupportModule.Interface.UnityDebugLog(line);
        }
    }
}