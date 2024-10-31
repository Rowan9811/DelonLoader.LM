using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Delon.Anti_Cheat.Patchs
{
    internal class HydrasPrivAntiCheatPatch
    {
        internal static void Install()
        {
            MelonLogger.Msg("\"HydrasPrivAntiCheatPatch\" is in the game");
            Core.HarmonyInstance.Patch(Type.GetType("HydrasPrivAntiCheatPatch").GetProperty("Update").GetGetMethod(), typeof(HydrasPrivAntiCheatPatch).GetMethod(nameof(Update)).ToNewHarmonyMethod());
        }
        public bool Update()
        {
            var bbc = 0;
            bbc++;
            if (bbc == 1) 
            { return false; }
            return false;
        }
    }
}
