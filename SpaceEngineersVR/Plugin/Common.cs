using SpaceEngineersVR.Util;
using SpaceEngineersVR.Config;
using System;
using System.Drawing;
using System.IO;
using ClientPlugin.Plugin;

namespace SpaceEngineersVR.Plugin
{
    public static class Common
    {
        public static IVRPlugin Plugin { get; private set; }
        public static IPluginConfig Config { get; private set; }

        public static readonly string Name = "SpaceEngineersVR";
        public static readonly string PublicName = "Space Engineers VR";
        public static readonly string ShortName = "SEVR";

        public static readonly Version Version = typeof(Main).Assembly.GetName().Version;

        public static readonly Icon Icon = new Icon(Path.Combine(Util.Util.GetAssetFolder(), "icon.ico"));
        public static readonly string IconPngPath = Path.Combine(Util.Util.GetAssetFolder(), "logo.png");
        public static readonly string IconIcoPath = Path.Combine(Util.Util.GetAssetFolder(), "logo.ico");

        public static readonly string ActionJsonPath = Path.Combine(Util.Util.GetAssetFolder(), "Controls", "actions.json");
        public static void SetPlugin(Main plugin)
        {
            Plugin = plugin;
            Config = plugin.Config;
        }
    }
}