using BepInEx.Configuration;

namespace MountYourFriends
{
    internal static class Settings
    {
        internal static ConfigEntry<float> SupportMoveUnmountThresholdMeters;
        internal static ConfigEntry<float> SupportYawUnmountThresholdDegrees;
        internal static ConfigEntry<float> SupportMoveCheckIntervalSeconds;
        internal static ConfigEntry<float> SupportDetectionRadiusMeters;

        internal static void Init(ConfigFile config)
        {
            SupportMoveUnmountThresholdMeters = config.Bind(
                "Support Tracking",
                "SupportMoveUnmountThresholdMeters",
                0.25f,
                "Unmount when the support player moves farther than this from their position when mounting started. Set to 0 to disable position checks.");

            SupportYawUnmountThresholdDegrees = config.Bind(
                "Support Tracking",
                "SupportYawUnmountThresholdDegrees",
                15f,
                "Unmount when the support player rotates farther than this from their yaw when mounting started. Set to 0 to disable yaw checks.");

            SupportMoveCheckIntervalSeconds = config.Bind(
                "Support Tracking",
                "SupportMoveCheckIntervalSeconds",
                0.05f,
                "How often mounted player supports are checked.");

            SupportDetectionRadiusMeters = config.Bind(
                "Support Tracking",
                "SupportDetectionRadiusMeters",
                0.2f,
                "Radius around the mount point used to resolve which player is being used as support.");
        }
    }
}
