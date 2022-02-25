using System;
using System.IO;
using System.Windows.Forms;
using SpaceEnginnersVR.GUI;
using SpaceEnginnersVR.Player;
using SpaceEnginnersVR.Wrappers;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEnginnersVR.Config;
using SpaceEnginnersVR.Logging;
using SpaceEnginnersVR.Patches;
using SpaceEnginnersVR.Plugin;
using Valve.VR;
using VRage;
using VRage.FileSystem;
using VRage.Plugins;
using VRageMath;

namespace SpaceEnginnersVR
{
    // ReSharper disable once UnusedType.Global
    public class Main : IPlugin, ICommonPlugin
    {
        public const string Name = "SpaceEngineersVR";
        public static Main Instance { get; private set; }
        
        public Harmony Harmony { get; private set; }

        public long Tick { get; private set; }

        public IPluginLogger Log => Logger;
        private static readonly IPluginLogger Logger = new PluginLogger(Name, true);

        public IPluginConfig Config => config?.Data;
        private PersistentConfig<PluginConfig> config;
        private static readonly string ConfigFileName = $"{Name}.cfg";

        private static readonly object InitializationMutex = new object();
        private static bool initialized;
        private static bool failed;

        private static Headset Headset;

        private Vector2I DesktopResolution;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;

            Log.Info("Loading");

            var configPath = Path.Combine(MyFileSystem.UserDataPath, ConfigFileName);
            config = PersistentConfig<PluginConfig>.Load(Log, configPath);

            Common.SetPlugin(this);

            if (!PatchHelpers.HarmonyPatchAll(Log, Harmony = new Harmony(Name)))
            {
                failed = true;
                return;
            }

            Log.Debug("Successfully loaded");
        }

        public void Dispose()
        {
            try
            {
                OpenVR.System?.AcknowledgeQuit_Exiting();
                Log.Info("Exiting OpenVR and closing threads");
            }
            catch (Exception ex)
            {
                Log.Critical(ex, "Dispose failed");
            }

            Instance = null;
        }

        public void Update()
        {
            EnsureInitialized();
            try
            {
                if (!failed || OpenVR.System != null)

                {
                    CustomUpdate();
                    Tick++;
                }
            }
            catch (Exception ex)
            {
                Log.Critical(ex, "Update failed");
                failed = true;
            }
        }

        private void EnsureInitialized()
        {
            lock (InitializationMutex)
            {
                if (initialized || failed)
                    return;

                Log.Info("Initializing");
                try
                {
                    Initialize();
                }
                catch (Exception ex)
                {
                    Log.Critical(ex, "Failed to initialize plugin");
                    failed = true;
                    return;
                }

                Log.Debug("Successfully initialized");
                initialized = true;
            }
        }

        private void Initialize()
        {
            if (!OpenVR.IsRuntimeInstalled())
            {
                Log.Critical("SpaceEngineersVR: OpenVR not found!");
                failed = true;
                return;
            }

            if (!OpenVR.IsHmdPresent())
            {
                Log.Error("SpaceEngineersVR: No VR headset found, please plug one in and reboot the game to play");
                failed = true;
                return;
            }

            Log.Info("Starting Steam OpenVR");
            EVRInitError error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            Log.Error($"Booting error = {error}");

            if (error != EVRInitError.None)
            {
                Log.Critical("Failed to connect to SteamVR!");
                failed = true;
                return;
            }

            Log.Info("De-Keenifying enviroment");
            Form GameWindow = (Form)AccessTools.Field(MyVRage.Platform.Windows.GetType(), "m_form").GetValue(MyVRage.Platform.Windows);
            GameWindow.Icon = Common.Icon;
            GameWindow.Text = Common.PublicName;
            GameWindow.AccessibleName = Common.PublicName;

            MyPerGameSettings.GameIcon = Common.IconIcoPath;
            MyPerGameSettings.BasicGameInfo.GameName = Common.PublicName;
            MyPerGameSettings.BasicGameInfo.ApplicationName = Common.PublicName;
            MyPerGameSettings.BasicGameInfo.SplashScreenImage = Common.IconPngPath;
            MyPerGameSettings.BasicGameInfo.GameAcronym = "SEVR";

            Log.Info("Creating VR environment");
            Headset = new Headset();
            Headset.CreatePopup("Booted successfully");

            MySession.AfterLoading += AfterLoadedWorld;
            MySession.OnUnloading += UnloadingWorld;

            DesktopResolution = MyRender11.Resolution;

            Log.Info("Cleaning up...");
        }

        private void CustomUpdate()
        {

        }

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyPluginConfigDialog());
        }

        public void AfterLoadedWorld()
        {
            Log.Info("Loading SE game");
            Headset.CreatePopup("Loaded Game");
        }

        public void UnloadingWorld()
        {
            MyRender11.Resolution = DesktopResolution;
            Log.Info("Unloading SE game");
            Headset.CreatePopup("Unloaded Game");
        }
    }
}