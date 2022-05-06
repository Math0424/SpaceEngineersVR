using ClientPlugin.Player.Components;
using ClientPlugin.Plugin;
using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineersVR.Config;
using SpaceEngineersVR.GUI;
using SpaceEngineersVR.Player;
using SpaceEngineersVR.Wrappers;
using System;
using System.IO;
using System.Reflection;
using Valve.VR;
using VRage.FileSystem;
using VRage.Plugins;
using VRage.Utils;
using VRageMath;

namespace SpaceEngineersVR.Plugin
{
    // ReSharper disable once UnusedType.Global
    public class Main : IPlugin, IVRPlugin
    {
        public Harmony Harmony { get; private set; }
        public IPluginConfig Config => config?.Data;

        private PersistentConfig<PluginConfig> config;
        private static readonly string ConfigFileName = $"{Common.Name}.cfg";

        private static bool failed;

        private Vector2I DesktopResolution;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            MyLog.Default.WriteLine("SpaceEngineersVR: starting...");
            var configPath = Path.Combine(MyFileSystem.UserDataPath, ConfigFileName);
            config = PersistentConfig<PluginConfig>.Load(configPath);

            Common.SetPlugin(this);

            try
            {
                if (!Initialize())
                {
                    failed = true;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("SpaceEngineersVR: Failed to start!");
                MyLog.Default.WriteLine(ex.Message);
                MyLog.Default.WriteLine(ex.StackTrace);
                return;
            }
        }

        public void Dispose()
        {
            if (!failed)
            {
                try
                {
                    OpenVR.System?.AcknowledgeQuit_Exiting();
                    Logger.Info("Exiting OpenVR and closing threads");
                }
                catch (Exception ex)
                {
                    Logger.Critical(ex, "Dispose failed");
                }
            }
        }

        public void Update()
        {
            if (!failed)
            {
                try
                {
                    CustomUpdate();
                }
                catch (Exception ex)
                {
                    Logger.Critical(ex, "Update failed");
                    failed = true;
                }
            }
        }

        private bool Initialize()
        {
            if (!OpenVR.IsRuntimeInstalled())
            {
                MyLog.Default.WriteLine("SpaceEngineersVR: OpenVR not found!");
                return false;
            }

            if (!OpenVR.IsHmdPresent())
            {
                MyLog.Default.WriteLine("SpaceEngineersVR: No VR headset found, please plug one in and reboot the game to play");
                return false;
            }

            Logger.Info("Starting Steam OpenVR");
            EVRInitError error = EVRInitError.None;
            OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            Logger.Error($"Booting error = {error}");

            if (error != EVRInitError.None)
            {
                Logger.Critical("Failed to connect to SteamVR!");
                return false;
            }

            Logger.Info("Starting enviroment");
            MyPerGameSettings.GameIcon = Common.IconIcoPath;
            MyPerGameSettings.BasicGameInfo.GameName = Common.PublicName;
            MyPerGameSettings.BasicGameInfo.ApplicationName = Common.PublicName;
            MyPerGameSettings.BasicGameInfo.SplashScreenImage = Common.IconPngPath;
            MyPerGameSettings.BasicGameInfo.GameAcronym = Common.ShortName;

            Logger.Info("Patching game");
            Harmony = new Harmony(Common.Name);
            Harmony.PatchAll(Assembly.GetExecutingAssembly());

            Util.InitialiseOnStartAttribute.FindAndInitialise();

            MySession.AfterLoading += AfterLoadedWorld;
            MySession.OnUnloading += UnloadingWorld;

            DesktopResolution = MyRender11.Resolution;

            Logger.Info("Finalizing...");
            return true;
        }

        private void CustomUpdate()
        {
            DeviceManager.UpdateMain();

            if (MySession.Static?.LocalCharacter != null &&
                !MySession.Static.LocalCharacter.Components.Contains(typeof(VRMovementComponent)))
            {
                MySession.Static.LocalCharacter.Components.Add(new VRMovementComponent());
                MySession.Static.LocalCharacter.Components.Add(new VRHandsComponent());
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            MyGuiSandbox.AddScreen(new MyPluginConfigDialog());
        }

        public void AfterLoadedWorld()
        {
            Logger.Info("Loading SE game");
            DeviceManager.Headset.CreatePopup("Loaded Game");
        }

        public void UnloadingWorld()
        {
            MyRender11.Resolution = DesktopResolution;
            Logger.Info("Unloading SE game");
            DeviceManager.Headset.CreatePopup("Unloaded Game");
        }
    }
}