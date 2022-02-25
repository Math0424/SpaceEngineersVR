using ClientPlugin.Utill;
using Shared.Config;
using Shared.Logging;
using System;
using System.Drawing;
using System.IO;


namespace Shared.Plugin
{
    public static class Common
    {
        public static ICommonPlugin Plugin { get; private set; }
        public static IPluginLogger Logger { get; private set; }
        public static IPluginConfig Config { get; private set; }

        public static readonly string PublicName = "Space Engineers VR";

        public static readonly Version Version = typeof(ClientPlugin.Plugin).Assembly.GetName().Version;

        public static readonly Icon Icon = new Icon(Path.Combine(Util.GetAssetFolder(), "icon.ico"));
        public static readonly string IconPngPath = Path.Combine(Util.GetAssetFolder(), "logo.png");
        public static readonly string IconIcoPath = Path.Combine(Util.GetAssetFolder(), "logo.ico");

        public static readonly string ActionJsonPath = Path.Combine(Path.Combine(Util.GetAssetFolder(), "Controller", "actions.json"));
        public static void SetPlugin(ICommonPlugin plugin)
        {
            Plugin = plugin;
            Logger = plugin.Log;
            Config = plugin.Config;
        }
    }
}