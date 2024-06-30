using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DelonLoader.anti_cheat_patchs
{
    internal class DllCheckerPatch
    {

        internal static void Install()
        {

            Core.HarmonyInstance.Patch(Type.GetType("DllChecker").GetProperty("Start").GetGetMethod(), typeof(DllCheckerPatch).GetMethod(nameof(Prefix)).ToNewHarmonyMethod());
        }
        public bool Prefix()
        {
            MelonLogger.Msg("\"DllChecker\" is in the game");
            return false;
        }

    }
}
