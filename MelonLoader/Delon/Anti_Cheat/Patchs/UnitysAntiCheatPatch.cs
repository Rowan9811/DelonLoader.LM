using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Delon.Anti_Cheat.Patchs
{
    internal class UnitysAntiCheatPatch
    {
        
        internal static void Install(string UnityType)
        {
            
            Core.HarmonyInstance.Patch(Type.GetType(UnityType).GetProperty("Start").GetGetMethod(), typeof(UnitysAntiCheatPatch).GetMethod(nameof(Start)).ToNewHarmonyMethod());
            var discordWebhookURL = "{discordWebhookURL}";
            Type.GetType(UnityType).GetField("discordWebhookURL").GetValue(discordWebhookURL);
            MelonLogger.Msg($"grabbed {discordWebhookURL} from {UnityType}(Haunt Unity's anti-cheat)");
        }
        public bool Start()
        {
            //MelonLogger.Msg("\"SignatureCheck\" is in the game");
            return false;
        }
    }
}
