using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineersVR.Common;
using SpaceEngineersVR.Config;
using SpaceEngineersVR.GUI;
using SpaceEngineersVR.Logging;
using SpaceEngineersVR.Patches;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Wrappers;
using System;
using System.IO;
using System.Windows.Forms;
using Valve.VR;
using VRage;
using VRage.FileSystem;
using VRage.Plugins;
using VRageMath;

namespace SpaceEngineersVR
{
    public class Plugin : IPlugin, ICommonPlugin
    {
        public const string Name = "SpaceEngineersVR";
        public static Plugin Instance { get; private set; }

        public Harmony Harmony { get; private set; }

        public long Tick { get; private set; }

        public IPluginLogger Log => Logger;
        private static readonly IPluginLogger Logger = new PluginLogger(Name);

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

            string configPath = Path.Combine(MyFileSystem.UserDataPath, ConfigFileName);
            config = PersistentConfig<PluginConfig>.Load(Log, configPath);

            Common.Common.SetPlugin(this);

            if (!PatchHelpers.HarmonyPatchAll(Log, Harmony = new Harmony(Name)))
            {
                failed = true;
                return;
            }

            Log.Debug("Successfully loaded");
        }

        public void Update()
        {
            EnsureInitialized();
            try
            {
                if (!failed)
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
                {
                    return;
                }

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
            GameWindow.Icon = Common.Common.Icon;
            GameWindow.Text = Common.Common.PublicName;
            GameWindow.AccessibleName = Common.Common.PublicName;

            MyPerGameSettings.GameIcon = Util.GetAssetFolder() + "icon.ico";
            MyPerGameSettings.BasicGameInfo.GameName = Common.Common.PublicName;
            MyPerGameSettings.BasicGameInfo.ApplicationName = Common.Common.PublicName;
            MyPerGameSettings.BasicGameInfo.SplashScreenImage = (Util.GetAssetFolder() + "logo.png");
            MyPerGameSettings.BasicGameInfo.GameAcronym = "SEVR";

            Log.Info("Creating VR enviroment");
            Headset = new Headset();
            Headset.CreatePopup("Booted successfully");

            MySession.AfterLoading += AfterLoadedWorld;
            MySession.OnUnloading += UnloadingWorld;

            DesktopResolution = MyRender11.Resolution;

            Log.Info("Cleaning up...");
        }

        private void CustomUpdate()
        {
            Headset.GameUpdate();
        }

        public void AfterLoadedWorld()
        {
            Log.Info("Loading SE game");
            Headset.CreatePopup("Loaded Game");
        }

        public void UnloadingWorld()
        {
            MyRender11.Resolution = DesktopResolution;
            PlayerAndCameraDisabler.DisablePlayerAndCameraMovement = false;
            Log.Info("Unloading SE game");
            Headset.CreatePopup("Unloaded Game");
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

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyPluginConfigDialog());
        }
    }
}