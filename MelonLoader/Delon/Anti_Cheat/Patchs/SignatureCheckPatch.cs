using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Delon.Anti_Cheat.Patchs
{
    internal class SignatureCheckPatch
    {
        internal static void Install()
        {
            Core.HarmonyInstance.Patch(Type.GetType("SignatureCheck").GetProperty("Start").GetGetMethod(), typeof(SignatureCheckPatch).GetMethod(nameof(Start)).ToNewHarmonyMethod());
        }
        public bool Start()
        {
            MelonLogger.Msg("\"SignatureCheck\" is in the game");
            return false;
        }
    }
}
