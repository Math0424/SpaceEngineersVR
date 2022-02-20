using HarmonyLib;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpaceEngineersVR.Patches
{
    public static class MouseMovementDisabler
    {
        public static bool DisableCameraMovementByMouse = false;
        static MouseMovementDisabler()
        {
            SpaceVR.Harmony.Patch(AccessTools.Method("Sandbox.Game.World.MyEntityRemoteController:RotateEntity"), new HarmonyMethod(typeof(MouseMovementDisabler), nameof(Prefix)));
        }

        public static bool Prefix()
        {
            if (DisableCameraMovementByMouse)
            {
                return false;
            }
            return true;
        }
    }
}
