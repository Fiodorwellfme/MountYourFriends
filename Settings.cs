using System.Collections.Generic;
using BepInEx.Configuration;

namespace MountYourFriends
{
    internal static class Settings
    {
        internal static readonly List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        internal static ConfigEntry<float> SupportMoveUnmountThresholdMeters;
        internal static ConfigEntry<float> SupportYawUnmountThresholdDegrees;
        internal static ConfigEntry<float> SupportMoveCheckIntervalSeconds;
        internal static ConfigEntry<float> SupportDetectionRadiusMeters;

        internal static ConfigEntry<float> MountHigherOffsetMeters;
        internal static ConfigEntry<float> MountLowerOffsetMeters;
        internal static ConfigEntry<float> MountedExtraPitchDegrees;
        internal static ConfigEntry<float> MountedExtraYawDegrees;

        internal static ConfigEntry<bool> DebugLogging;

        internal static void Init(ConfigFile config)
        {
            ConfigEntries.Clear();

            // --- Support Tracking ---
            ConfigEntries.Add(SupportMoveUnmountThresholdMeters = config.Bind("Support Tracking", "Support Move Unmount Threshold", 0.25f,
                new ConfigDescription(
                    "Unmount when the support player moves farther than this from their position when mounting started. 0 disables position checks.",
                    new AcceptableValueRange<float>(0f, 5f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(SupportYawUnmountThresholdDegrees = config.Bind("Support Tracking", "Support Yaw Unmount Threshold", 15f,
                new ConfigDescription(
                    "Unmount when the support player rotates farther than this from their yaw when mounting started. 0 disables yaw checks.",
                    new AcceptableValueRange<float>(0f, 180f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(SupportMoveCheckIntervalSeconds = config.Bind("Support Tracking", "Support Move Check Interval", 0.05f,
                new ConfigDescription(
                    "How often mounted player supports are checked.",
                    new AcceptableValueRange<float>(0.01f, 1f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(SupportDetectionRadiusMeters = config.Bind("Support Tracking", "Support Detection Radius", 0.2f,
                new ConfigDescription(
                    "Radius around the mount point used to resolve which player is being used as support.",
                    new AcceptableValueRange<float>(0.01f, 2f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            // --- Mounting ---
            ConfigEntries.Add(MountHigherOffsetMeters = config.Bind("Mounting", "Detection Higher Offset", 0f,
                new ConfigDescription(
                    "Extra upward mount detection range in meters.",
                    new AcceptableValueRange<float>(0f, 2f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MountLowerOffsetMeters = config.Bind("Mounting", "Detection Lower Offset", 0f,
                new ConfigDescription(
                    "Extra downward mount detection range in meters.",
                    new AcceptableValueRange<float>(0f, 2f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MountedExtraPitchDegrees = config.Bind("Mounting", "Extra Pitch", 30f,
                new ConfigDescription(
                    "Extra mounted pitch range in degrees, added both up and down.",
                    new AcceptableValueRange<float>(0f, 90f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MountedExtraYawDegrees = config.Bind("Mounting", "Extra Yaw", 180f,
                new ConfigDescription(
                    "Extra mounted yaw range in degrees, added both left and right.",
                    new AcceptableValueRange<float>(0f, 180f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            // --- Debug ---
            ConfigEntries.Add(DebugLogging = config.Bind("Debug", "Debug Logging", false,
                new ConfigDescription(
                    "Log mount support tracking diagnostics.",
                    null,
                    new global::ConfigurationManagerAttributes { IsAdvanced = true })));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            int settingOrder = ConfigEntries.Count;
            foreach (ConfigEntryBase entry in ConfigEntries)
            {
                global::ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as global::ConfigurationManagerAttributes;
                if (attributes != null)
                    attributes.Order = settingOrder;

                settingOrder--;
            }
        }
    }
}
