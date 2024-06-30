using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelonLoader.anti_cheat_patchs
{
    internal class UnitysAntiCheatPatch
    {
        
        internal static void Install(string UnityType)
        {
            
            Core.HarmonyInstance.Patch(Type.GetType(UnityType).GetProperty("Start").GetGetMethod(), typeof(SignatureCheckPatch).GetMethod(nameof(Prefix)).ToNewHarmonyMethod());
            var discordWebhookURL = "{discordWebhookURL}";
            Type.GetType(UnityType).GetField("discordWebhookURL").GetValue(discordWebhookURL);
            MelonLogger.Msg($"grabbed {discordWebhookURL} from {UnityType}(Haunt Unity's anti-cheat)");
        }
        public bool Prefix()
        {
            //MelonLogger.Msg("\"SignatureCheck\" is in the game");
            return false;
        }
    }
}
