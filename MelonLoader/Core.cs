using System;
using System.Diagnostics;
using MelonLoader.InternalUtils;
using MelonLoader.MonoInternals;
using bHapticsLib;
using System.IO;
using System.Security.Permissions;
using System.Reflection;
using DelonLoader.anti_cheat_patchs;
#pragma warning disable IDE0051 // Prevent the IDE from complaining about private unreferenced methods

namespace MelonLoader
{
    [ReflectionPermission(SecurityAction.Deny)]
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
            foreach (var file in Directory.GetFiles(MelonUtils.UserLibsDirectory, "*.dll"))
            {
                try
                {
                    System.Reflection.Assembly.LoadFrom(file);
                    MelonDebug.Msg("Loaded " + Path.GetFileName(file) + " from UserLibs!");
                }
                catch (Exception e)
                {
                    MelonLogger.Msg("Failed to load " + Path.GetFileName(file) + " from UserLibs!");
                    MelonLogger.Error(e.ToString());
                }
            }
#endif
            bool bypassHarmony = false;
            if (File.Exists(Path.Combine(MelonUtils.BaseDirectory, "isEmulator.txt")))
            {
                bypassHarmony = true;
                // Tells Harmony that it already did some internal patching junk so that I don't have to modify the code myself
                typeof(HarmonyLib.Traverse).Assembly.GetType("HarmonyLib.Internal.RuntimeFixes.StackTraceFixes").GetField("_applied", HarmonyLib.AccessTools.all).SetValue(null, true);
            }

            HarmonyInstance = new HarmonyLib.Harmony(BuildInfo.Name);

            if (!bypassHarmony)
            {
                Fixes.ForcedCultureInfo.Install();
                Fixes.InstancePatchFix.Install();
                Fixes.ProcessFix.Install();
#if __ANDROID__
                Fixes.DateTimeOverride.Install();
                Fixes.LemonCrashFixes.Install();
#endif
#if !__ANDROID__
                PatchShield.Install();
#endif
                
                //try
                //{
                    //todo try to figure out a faster way
                    foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foreach (Type type in a.GetTypes())
                        {

                            switch (type.Name)
                            {
                                case "QuestLink":
                                    {
                                        if (type.GetField("useWebhook") == null)
                                            QuestLinkPatch.Install();
                                        break;
                                    }
                                case "DllChecker":
                                    {
                                        if (type.GetField("useWebhook") == null)
                                            DllCheckerPatch.Install();
                                        break;
                                    }
                                case "KSHRAnti":
                                    {
                                        if (type.GetField("useWebhook") == null)
                                            KSHRAntiPatch.Install();
                                        break;
                                    }
                                case "SignatureCheck":
                                    {
                                        if (type.GetField("useWebhook") == null)
                                            SignatureCheckPatch.Install();
                                        break;
                                    }
                            }

                            //check for unity's(Haunt Unity) anti-cheat
                            var m = type.GetMethods();
                            var f = type.GetFields();
                            if (m[0].Name == "Start" && m[1].Name == "CheckForBlockedFolders" && m[2].Name == "SendDiscordWarning" && m[3].Name == "QuitGame")
                            {
                                if (f[0].Name == "blockedFolders" && f[1].Name == "useWebhook" && f[2].Name == "discordWebhookURL" && f[3].Name == "webhookMessage")
                                {
                                    string discordWebhookURL = "";
                                    f[2].GetValue(discordWebhookURL);
                                    MelonLogger.Msg($"grabbed {discordWebhookURL} from {type.Name}(Haunt Unity's anti-cheat)");
                                    UnitysAntiCheatPatch.Install(type.Name);
                                }
                            }

                            //if (type.Name == "QuestLink")
                            //{
                            //    QuestLinkPatch.Install();
                            //}
                        }
                    }
                //}
                //catch (SystemException exc)
                //{
                    //MelonLogger.Error(exc);
                    //MelonLogger.Error(exc.Message);
                    //MelonLogger.Error(exc.StackTrace);
                //}
                
            }

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
            if (MelonLaunchOptions.Core.DoModifiedLog)
                AddUnityDebugLog();

            RegisterTypeInIl2Cpp.SetReady();

            MelonEvents.MelonHarmonyInit.Invoke();
            MelonEvents.OnApplicationStart.Invoke();
            //todo try to figure out how to get this shit working
            //console_hook.Core.ints();

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

        private static void Pause()
        {
            MelonPreferences.Save();
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