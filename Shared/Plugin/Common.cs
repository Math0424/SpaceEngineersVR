using ClientPlugin.Utill;
using Shared.Config;
using Shared.Logging;
using System;
using System.Drawing;

namespace Shared.Plugin
{
    public static class Common
    {
        public static ICommonPlugin Plugin { get; private set; }
        public static IPluginLogger Logger { get; private set; }
        public static IPluginConfig Config { get; private set; }

        public static readonly Version Version = typeof(ClientPlugin.Plugin).Assembly.GetName().Version;
        public static readonly Icon Icon = new Icon(Util.GetAssetFolder() + "icon.ico");
        public static readonly string PublicName = "Space Engineers VR";

        public static void SetPlugin(ICommonPlugin plugin)
        {
            Plugin = plugin;
            Logger = plugin.Log;
            Config = plugin.Config;
        }
    }
}