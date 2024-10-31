using DelonLoader.anti_cheat_patchs;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DelonLoader
{
    public class AntiCheatPatcher
    {
        internal static List<Type> tys;

        internal static void Patch()
        {
            if (tys == null) { tys = new List<Type>(); }
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
                                {
                                    QuestLinkPatch.Install();
                                    tys.Add(type);
                                }
                                break;
                            }
                        case "DllChecker":
                            {
                                if (type.GetField("useWebhook") == null)
                                {
                                    DllCheckerPatch.Install();
                                    tys.Add(type);
                                }
                                break;
                            }
                        case "KSHRAnti":
                            {
                                if (type.GetField("useWebhook") == null)
                                {
                                    KSHRAntiPatch.Install();
                                    tys.Add(type);
                                }
                                break;
                            }
                        case "SignatureCheck":
                            {
                                if (type.GetField("useWebhook") == null)
                                {
                                    SignatureCheckPatch.Install();
                                    tys.Add(type);
                                }
                                break;
                            }
                        case "HydrasPrivAntiCheat":
                            {
                                if (type.GetField("useWebhook") == null)
                                {
                                    HydrasPrivAntiCheatPatch.Install();
                                    tys.Add(type);
                                }
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
                            tys.Add(type);
                        }
                    }

                    //if (type.Name == "QuestLink")
                    //{
                    //    QuestLinkPatch.Install();
                    //}
                }
            }
        }

        public static Type[] GetAntiCheats()
        {
            return tys.ToArray();
        }

    }
}
