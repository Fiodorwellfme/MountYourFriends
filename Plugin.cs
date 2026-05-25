using BepInEx;
using BepInEx.Logging;
using MountYourFriends.Patches;

namespace MountYourFriends
{
    [BepInPlugin("com.fiodor.MountYourFriends", "Mount Your Friends", "1.1.0")]
    public sealed class MountYourFriends : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource;

        private void Awake()
        {
            LogSource = Logger;
            Settings.Init(Config);
            new MountingPointLayerMaskPatch().Enable();
            new MountingPointDetectionRangePatch().Enable();
            new MountingMovementSettingsPatch().Enable();
            new MountedBodyOrbitEnterPatch().Enable();
            new MountedBodyOrbitRotatePatch().Enable();
            new MountingSupportCapturePatch().Enable();
            LogSource.LogInfo("Mount Your Friends 1.1.0 loaded.");
        }

        private void Update()
        {
            SupportMountTracker.Tick(UnityEngine.Time.deltaTime);
        }
    }
}
