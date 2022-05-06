using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character.Components;
using Sandbox.Game.Screens.Helpers.RadialMenuActions;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using SpaceEngineersVR.Player;
using System.Runtime.CompilerServices;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace ClientPlugin.Player.Components
{

    public static class VRGUIManager
    {

        static VRGUIManager()
        {
        
        }

        //called in Headset draw method
        //so make sure its fast :)
        public static void Draw(Matrix viewMatrix)
        {
            //TODO draw GUI stuffs
        }

    }
}