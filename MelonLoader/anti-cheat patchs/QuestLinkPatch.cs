using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using MelonLoader.Fixes;
using MelonLoader;

namespace DelonLoader.anti_cheat_patchs
{
    public class QuestLinkPatch
    {
        internal static void Install()
        {
            Core.HarmonyInstance.Patch(Type.GetType("QuestLink").GetProperty("Start").GetGetMethod(), typeof(QuestLinkPatch).GetMethod(nameof(Prefix)).ToNewHarmonyMethod());
        }
        public bool Prefix()
        {
            MelonLogger.Msg("\"QuestLink\" is in the game");
            return false;
        }

    }
}
