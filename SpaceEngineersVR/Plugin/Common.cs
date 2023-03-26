using SpaceEngineersVR.Config;
using System;
using System.Drawing;
using System.IO;

namespace SpaceEngineersVR.Plugin
{
    public static class Common
    {
        public static Main Plugin
        {
            get; private set;
        }
        public static PluginConfig Config
        {
            get; private set;
        }

        public static readonly string Name = "SpaceEngineersVR";
        public static readonly string PublicName = "Space Engineers VR";
        public static readonly string ShortName = "SEVR";

        public static readonly Version Version = typeof(Main).Assembly.GetName().Version;

        public static string AssetFolder
        {
            get; private set;
        }

        public static Icon Icon
        {
            get; private set;
        }
        public static string IconPngPath
        {
            get; private set;
        }
        public static string IconIcoPath
        {
            get; private set;
        }
        public static string ActionJsonPath
        {
            get; private set;
        }

        public static void SetPlugin(Main plugin)
        {
            Plugin = plugin;
            Config = plugin.Config;
            if (AssetFolder == null)
                SetAssetPath(Util.Util.GetDefaultAssetFolder());
        }

        public static void SetAssetPath(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                throw new ArgumentException("Folder must not be null", folder);
            if (!Directory.Exists(folder))
                throw new DirectoryNotFoundException("Asset folder not found");
            AssetFolder = folder;
            Icon = new Icon(Path.Combine(folder, "icon.ico"));
            IconPngPath = Path.Combine(folder, "logo.png");
            IconIcoPath = Path.Combine(folder, "logo.ico");
            ActionJsonPath = Path.Combine(folder, "Controls", "actions.json");
        }
    }
}