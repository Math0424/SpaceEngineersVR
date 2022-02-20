using HarmonyLib;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineersVR.Patches
{
    public static class PlayerAndCameraDisabler
    {
        public static bool DisablePlayerAndCameraMovement = false;
        static PlayerAndCameraDisabler()
        {
            SpaceVR.Harmony.Patch(AccessTools.Method("Sandbox.Game.Gui.MyGuiScreenGamePlay:MoveAndRotatePlayerOrCamera"), new HarmonyMethod(typeof(PlayerAndCameraDisabler), nameof(Prefix)));
        }

        public static bool Prefix()
        {
            if (DisablePlayerAndCameraMovement)
            {
                return false;
            }
            return true;
        }
    }
}
