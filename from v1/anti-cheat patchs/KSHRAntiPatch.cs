using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelonLoader.anti_cheat_patchs
{
    internal class KSHRAntiPatch
    {
        internal static void Install()
        {
            Core.HarmonyInstance.Patch(Type.GetType("KSHRAnti").GetProperty("Start").GetGetMethod(), typeof(KSHRAntiPatch).GetMethod(nameof(Start)).ToNewHarmonyMethod());
        }
        public bool Start()
        {
            MelonLogger.Msg("\"KSHRAnti\" is in the game");
            return false;
        }
    }
}
