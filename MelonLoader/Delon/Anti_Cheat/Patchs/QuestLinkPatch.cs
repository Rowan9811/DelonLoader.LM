using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MelonLoader.Fixes;
using MelonLoader;
using System.Collections;

namespace Delon.Anti_Cheat.Patchs
{
    public class QuestLinkPatch
    {
        internal static void Install()
        {
            Core.HarmonyInstance.Patch(Type.GetType("QuestLink").GetProperty("Start").GetGetMethod(), typeof(QuestLinkPatch).GetMethod(nameof(Start)).ToNewHarmonyMethod());
        }
        public bool Start()
        {
            MelonLogger.Msg("\"QuestLink\" is in the game");
            return false;
        }

    }
}
