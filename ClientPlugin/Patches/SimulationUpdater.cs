using System;
using VRage.Game;
using VRage.Game.Components;
#pragma warning disable CS0649

namespace ClientPlugin.Patches
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    internal class SimulationUpdater : MySessionComponentBase
    {
        public static Action UpdateBeforeSim;
        public static Action UpdateAfterSim;
        public static Action UpdateSim;
        public static Action OnWorldUnload;
        public static Action BeforeWorldStart;

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {

        }

        public override void BeforeStart()
        {
            BeforeWorldStart?.Invoke();
        }

        public override void UpdateBeforeSimulation()
        {
            UpdateBeforeSim?.Invoke();
        }

        public override void Simulate()
        {
            UpdateSim?.Invoke();
        }

        public override void UpdateAfterSimulation()
        {
            UpdateAfterSim?.Invoke();
        }

        protected override void UnloadData()
        {
            OnWorldUnload?.Invoke();
        }
    }
}
