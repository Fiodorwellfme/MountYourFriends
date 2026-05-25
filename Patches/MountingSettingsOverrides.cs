using System.Collections.Generic;
using EFT.WeaponMounting;
using UnityEngine;

namespace MountYourFriends.Patches
{
    internal static class MountingSettingsOverrides
    {
        private static readonly Dictionary<IMountingPointDetectionSettings, DetectionSettingsSnapshot> DetectionDefaults = new Dictionary<IMountingPointDetectionSettings, DetectionSettingsSnapshot>();
        private static readonly Dictionary<IMountingMovementSettings, MovementSettingsSnapshot> MovementDefaults = new Dictionary<IMountingMovementSettings, MovementSettingsSnapshot>();

        internal static void ApplyDetectionSettings(IMountingPointDetectionSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (!DetectionDefaults.TryGetValue(settings, out DetectionSettingsSnapshot defaults))
            {
                defaults = new DetectionSettingsSnapshot(settings);
                DetectionDefaults.Add(settings, defaults);
            }

            float higherOffset = Mathf.Max(0f, Settings.MountHigherOffsetMeters.Value);
            float lowerOffset = Mathf.Max(0f, Settings.MountLowerOffsetMeters.Value);

            settings.GridMinHeight = defaults.GridMinHeight - lowerOffset;
            settings.GridMaxHeight = defaults.GridMaxHeight + higherOffset;
            settings.VerticalGridSize = defaults.VerticalGridSize + higherOffset + lowerOffset;
        }

        internal static void ApplyMovementSettings(IMountingMovementSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            if (!MovementDefaults.TryGetValue(settings, out MovementSettingsSnapshot defaults))
            {
                defaults = new MovementSettingsSnapshot(settings);
                MovementDefaults.Add(settings, defaults);
            }

            float extraPitch = Mathf.Max(0f, Settings.MountedExtraPitchDegrees.Value);
            float extraYaw = GetExtraYawDegrees();

            settings.PitchLimitVertical = Expand(defaults.PitchLimitVertical, extraPitch);
            settings.PitchLimitHorizontal = Expand(defaults.PitchLimitHorizontal, extraPitch);
            settings.PitchLimitHorizontalBipod = Expand(defaults.PitchLimitHorizontalBipod, extraPitch);
            settings.MaxPitchLimitExcess = defaults.MaxPitchLimitExcess + extraPitch;
            settings.MaxVerticalMountAngle = defaults.MaxVerticalMountAngle + extraYaw;
            settings.MaxYawLimitExcess = defaults.MaxYawLimitExcess + extraYaw;
        }

        internal static float GetExtraYawDegrees()
        {
            return Mathf.Max(0f, Settings.MountedExtraYawDegrees.Value);
        }

        private static Vector2 Expand(Vector2 value, float amount)
        {
            return new Vector2(value.x - amount, value.y + amount);
        }

        private readonly struct DetectionSettingsSnapshot
        {
            internal readonly float GridMinHeight;
            internal readonly float GridMaxHeight;
            internal readonly float VerticalGridSize;

            internal DetectionSettingsSnapshot(IMountingPointDetectionSettings settings)
            {
                GridMinHeight = settings.GridMinHeight;
                GridMaxHeight = settings.GridMaxHeight;
                VerticalGridSize = settings.VerticalGridSize;
            }
        }

        private readonly struct MovementSettingsSnapshot
        {
            internal readonly float MaxVerticalMountAngle;
            internal readonly Vector2 PitchLimitVertical;
            internal readonly Vector2 PitchLimitHorizontal;
            internal readonly Vector2 PitchLimitHorizontalBipod;
            internal readonly float MaxPitchLimitExcess;
            internal readonly float MaxYawLimitExcess;

            internal MovementSettingsSnapshot(IMountingMovementSettings settings)
            {
                MaxVerticalMountAngle = settings.MaxVerticalMountAngle;
                PitchLimitVertical = settings.PitchLimitVertical;
                PitchLimitHorizontal = settings.PitchLimitHorizontal;
                PitchLimitHorizontalBipod = settings.PitchLimitHorizontalBipod;
                MaxPitchLimitExcess = settings.MaxPitchLimitExcess;
                MaxYawLimitExcess = settings.MaxYawLimitExcess;
            }
        }
    }
}
