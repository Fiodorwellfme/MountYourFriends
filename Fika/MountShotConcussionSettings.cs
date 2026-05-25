using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace MountYourFriendsFika
{
    internal static class MountShotConcussionSettings
    {
        internal static readonly List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();
        private static readonly Dictionary<string, ConfigEntry<float>> CaliberAudibleDistances = new Dictionary<string, ConfigEntry<float>>();

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> MaxAudibleDistanceMeters;
        internal static ConfigEntry<float> MinimumDurationSeconds;
        internal static ConfigEntry<float> MaximumDurationSeconds;
        internal static ConfigEntry<float> MinimumIntensity;
        internal static ConfigEntry<float> MaximumIntensity;
        internal static ConfigEntry<float> MinimumDurationIncreasePerShotSeconds;
        internal static ConfigEntry<float> MaximumDurationIncreasePerShotSeconds;
        internal static ConfigEntry<float> MinimumIntensityIncreasePerShot;
        internal static ConfigEntry<float> MaximumIntensityIncreasePerShot;
        internal static ConfigEntry<float> LowMuteReduction;
        internal static ConfigEntry<float> StrongMuteReduction;
        internal static ConfigEntry<float> SuppressorLoudnessReductionMultiplier;
        internal static ConfigEntry<float> GunsCompressorHighLevel;
        internal static ConfigEntry<float> GunsCompressorLowLevel;
        internal static ConfigEntry<float> MaxDoubleVisionAmountEntry;
        internal static ConfigEntry<float> MaxWiggleSpeedEntry;
        internal static ConfigEntry<float> MaxWiggleScaleEntry;
        internal static ConfigEntry<float> MaxWiggleStrengthEntry;
        internal static ConfigEntry<float> MaxMotionBlurAmountEntry;
        internal static ConfigEntry<bool> DebugLogging;

        internal static float MaxDoubleVisionAmount { get; private set; }
        internal static float MaxWiggleSpeed { get; private set; }
        internal static float MaxWiggleScale { get; private set; }
        internal static float MaxWiggleStrength { get; private set; }
        internal static float MaxMotionBlurAmount { get; private set; }

        internal static void Init(ConfigFile config)
        {
            ConfigEntries.Clear();
            CaliberAudibleDistances.Clear();

            ConfigEntries.Add(Enabled = config.Bind("General", "Enable Concussion/Tinnitus", true,
                new ConfigDescription(
                    "Enable mounted shot concussion and tinnitus handling.",
                    null,
                    new global::ConfigurationManagerAttributes { IsAdvanced = false })));

            ConfigEntries.Add(MaxAudibleDistanceMeters = config.Bind("Mount Shot Concussion", "Max Audible Distance", 400f,
                new ConfigDescription(
                    "Audible distance that maps to maximum per-shot concussion duration and intensity.",
                    new AcceptableValueRange<float>(1f, 1000f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MinimumDurationSeconds = config.Bind("Mount Shot Concussion", "Minimum Duration", 2f,
                new ConfigDescription(
                    "Minimum total contusion duration after a mounted shot applies.",
                    new AcceptableValueRange<float>(0f, 120f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MaximumDurationSeconds = config.Bind("Mount Shot Concussion", "Maximum Duration", 10f,
                new ConfigDescription(
                    "Maximum total contusion duration after accumulated mounted shots.",
                    new AcceptableValueRange<float>(0f, 120f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MinimumIntensity = config.Bind("Mount Shot Concussion", "Minimum Intensity", 0.05f,
                new ConfigDescription(
                    "Minimum total contusion intensity after a mounted shot applies.",
                    new AcceptableValueRange<float>(0f, 50f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MaximumIntensity = config.Bind("Mount Shot Concussion", "Maximum Intensity", 10f,
                new ConfigDescription(
                    "Maximum total contusion intensity after accumulated mounted shots.",
                    new AcceptableValueRange<float>(0f, 50f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MinimumDurationIncreasePerShotSeconds = config.Bind("Mount Shot Concussion", "Minimum Duration Increase Per Shot", 0.5f,
                new ConfigDescription(
                    "Minimum duration added by each mounted shot before protection reduction.",
                    new AcceptableValueRange<float>(0f, 30f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MaximumDurationIncreasePerShotSeconds = config.Bind("Mount Shot Concussion", "Maximum Duration Increase Per Shot", 2f,
                new ConfigDescription(
                    "Maximum duration added by each mounted shot before protection reduction.",
                    new AcceptableValueRange<float>(0f, 30f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MinimumIntensityIncreasePerShot = config.Bind("Mount Shot Concussion", "Minimum Intensity Increase Per Shot", 0.1f,
                new ConfigDescription(
                    "Minimum intensity added by each mounted shot before protection reduction.",
                    new AcceptableValueRange<float>(0f, 10f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MaximumIntensityIncreasePerShot = config.Bind("Mount Shot Concussion", "Maximum Intensity Increase Per Shot", 1f,
                new ConfigDescription(
                    "Maximum intensity added by each mounted shot before protection reduction.",
                    new AcceptableValueRange<float>(0f, 10f),
                    new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(CaliberAudibleDistances["20x1mm"] = config.Bind("Mount Shot Concussion Calibers", "20x1mm disk (Blicky)", 5000f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["9x18PM"] = config.Bind("Mount Shot Concussion Calibers", "9x18 mm", 110f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["9x19PARA"] = config.Bind("Mount Shot Concussion Calibers", "9x19mm Parabellum", 110f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["46x30"] = config.Bind("Mount Shot Concussion Calibers", "4.6 x 30mm", 120f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["9x21"] = config.Bind("Mount Shot Concussion Calibers", "9x21mm Gyurza", 120f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["57x28"] = config.Bind("Mount Shot Concussion Calibers", "5.7 x 28 mm", 120f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x25TT"] = config.Bind("Mount Shot Concussion Calibers", "7.62 x 25 mm Tokarev", 120f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["1143x23ACP"] = config.Bind("Mount Shot Concussion Calibers", ".45 ACP", 115f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["9x33R"] = config.Bind("Mount Shot Concussion Calibers", ".357 Magnum", 125f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["545x39"] = config.Bind("Mount Shot Concussion Calibers", "5.45 x 39 mm", 160f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["556x45NATO"] = config.Bind("Mount Shot Concussion Calibers", "5.56 x 45 mm", 160f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["9x39"] = config.Bind("Mount Shot Concussion Calibers", "9x39mm", 160f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x35"] = config.Bind("Mount Shot Concussion Calibers", ".300 Blackout", 175f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x39"] = config.Bind("Mount Shot Concussion Calibers", "7.62 x 39 mm", 175f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["366TKM"] = config.Bind("Mount Shot Concussion Calibers", ".366 TKM", 175f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x51"] = config.Bind("Mount Shot Concussion Calibers", "7.62 x 51 mm", 200f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["127x55"] = config.Bind("Mount Shot Concussion Calibers", "12.7x55mm", 200f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x54R"] = config.Bind("Mount Shot Concussion Calibers", "7.62 x 54 mm R", 225f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["86x70"] = config.Bind("Mount Shot Concussion Calibers", ".338 Lapua Magnum", 250f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["20g"] = config.Bind("Mount Shot Concussion Calibers", "20 gauge", 185f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["12g"] = config.Bind("Mount Shot Concussion Calibers", "12 gauge", 185f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["23x75"] = config.Bind("Mount Shot Concussion Calibers", "23 x 75 mm (KS-23)", 210f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["26x75"] = config.Bind("Mount Shot Concussion Calibers", "Flare Shell", 50f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["30x29"] = config.Bind("Mount Shot Concussion Calibers", "30x29mm VOG-30", 50f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["40x46"] = config.Bind("Mount Shot Concussion Calibers", "40x46mm", 50f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["40mmRU"] = config.Bind("Mount Shot Concussion Calibers", "40mm VOG-25", 50f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["127x99"] = config.Bind("Mount Shot Concussion Calibers", ".50 BMG", 275f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["127x108"] = config.Bind("Mount Shot Concussion Calibers", "12.7x108mm", 300f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["725"] = config.Bind("Mount Shot Concussion Calibers", "RShG-2 72.5mm rocket launcher", 400f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["1036x77"] = config.Bind("Mount Shot Concussion Calibers", ".408 Cheyenne Tactical", 250f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["11x33R"] = config.Bind("Mount Shot Concussion Calibers", ".44 Magnum", 150f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["25x59"] = config.Bind("Mount Shot Concussion Calibers", "25x59 mm", 250f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["762x67B"] = config.Bind("Mount Shot Concussion Calibers", ".300 Winchester Magnum", 200f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["784x49"] = config.Bind("Mount Shot Concussion Calibers", ".308 Marlin Express", 200f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["792x57"] = config.Bind("Mount Shot Concussion Calibers", "7.92x57mm Mauser", 225f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["86x43"] = config.Bind("Mount Shot Concussion Calibers", "8.6mm Blackout", 200f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["86x63"] = config.Bind("Mount Shot Concussion Calibers", ".338 Norma Magnum", 250f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["93x64"] = config.Bind("Mount Shot Concussion Calibers", "9.3x64mm Brenneke", 240f, CaliberDescription()));
            ConfigEntries.Add(CaliberAudibleDistances["68x51"] = config.Bind("Mount Shot Concussion Calibers", "6.8x51mm SIG", 200f, CaliberDescription()));

            ConfigEntries.Add(LowMuteReduction = config.Bind("Mount Shot Concussion Protection", "Low Mute Reduction", 0.35f,
                new ConfigDescription("Concussion reduction from Low helmet deafness.", new AcceptableValueRange<float>(0f, 1f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = true })));
            ConfigEntries.Add(StrongMuteReduction = config.Bind("Mount Shot Concussion Protection", "Strong Mute Reduction", 0.65f,
                new ConfigDescription("Concussion reduction from High helmet deafness.", new AcceptableValueRange<float>(0f, 1f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = true })));
            ConfigEntries.Add(SuppressorLoudnessReductionMultiplier = config.Bind("Mount Shot Concussion Protection", "Suppressor Loudness Reduction Multiplier", 1f,
                new ConfigDescription("Multiplier applied to negative suppressor Loudness when converting it to concussion/tinnitus reduction. 1 = -30 Loudness gives 30% reduction.", new AcceptableValueRange<float>(0f, 100f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(GunsCompressorHighLevel = config.Bind("Mount Shot Concussion Protection", "Guns Compressor High Level", 0f,
                new ConfigDescription("GunsCompressorSendLevel value that maps to no headset concussion reduction.", new AcceptableValueRange<float>(-80f, 20f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(GunsCompressorLowLevel = config.Bind("Mount Shot Concussion Protection", "Guns Compressor Low Level", -14f,
                new ConfigDescription("GunsCompressorSendLevel value that maps to full headset concussion reduction.", new AcceptableValueRange<float>(-80f, 20f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(MaxDoubleVisionAmountEntry = config.Bind("Mount Shot Concussion Visuals", "Max Double Vision Amount", 1f,
                new ConfigDescription("Maximum double vision amount applied by mounted shot contusion visuals.", new AcceptableValueRange<float>(0f, 20f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(MaxWiggleSpeedEntry = config.Bind("Mount Shot Concussion Visuals", "Max Wiggle Speed", 50f,
                new ConfigDescription("Maximum screen warble speed applied by mounted shot contusion visuals.", new AcceptableValueRange<float>(0f, 50f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(MaxWiggleScaleEntry = config.Bind("Mount Shot Concussion Visuals", "Max Wiggle Scale", 4f,
                new ConfigDescription("Maximum screen warble scale applied by mounted shot contusion visuals.", new AcceptableValueRange<float>(0f, 100f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(MaxWiggleStrengthEntry = config.Bind("Mount Shot Concussion Visuals", "Max Wiggle Strength", 0.8f,
                new ConfigDescription("Maximum screen warble strength applied by mounted shot contusion visuals.", new AcceptableValueRange<float>(0f, 5f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));
            ConfigEntries.Add(MaxMotionBlurAmountEntry = config.Bind("Mount Shot Concussion Visuals", "Max Motion Blur Amount", 20f,
                new ConfigDescription("Maximum motion blur amount applied by mounted shot contusion visuals.", new AcceptableValueRange<float>(0f, 20f), new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false })));

            ConfigEntries.Add(DebugLogging = config.Bind("Debug", "Mount Shot Concussion Debug Logging", true,
                new ConfigDescription("Log why mounted shot concussion did or did not apply.", null, new global::ConfigurationManagerAttributes { IsAdvanced = true })));

            MaxDoubleVisionAmountEntry.SettingChanged += OnSettingsChanged;
            MaxWiggleSpeedEntry.SettingChanged += OnSettingsChanged;
            MaxWiggleScaleEntry.SettingChanged += OnSettingsChanged;
            MaxWiggleStrengthEntry.SettingChanged += OnSettingsChanged;
            MaxMotionBlurAmountEntry.SettingChanged += OnSettingsChanged;

            RecalcOrder();
            Rebuild();
        }

        internal static bool TryGetAudibleDistance(string caliber, out float audibleDistance)
        {
            audibleDistance = 0f;
            if (string.IsNullOrEmpty(caliber))
                return false;

            return CaliberAudibleDistances.TryGetValue(NormalizeCaliber(caliber), out ConfigEntry<float> entry)
                && (audibleDistance = Math.Max(0f, entry.Value)) > 0f;
        }

        internal static void CalculateConcussion(float audibleDistance, float reduction, out float additionalDuration, out float additionalIntensity, out float minimumDuration, out float maximumDuration, out float minimumIntensity, out float maximumIntensity)
        {
            if (!Enabled.Value)
            {
                additionalDuration = 0f;
                additionalIntensity = 0f;
                minimumDuration = 0f;
                maximumDuration = 0f;
                minimumIntensity = 0f;
                maximumIntensity = 0f;
                return;
            }

            float normalizedDistance = Mathf.Clamp01(audibleDistance / Math.Max(1f, MaxAudibleDistanceMeters.Value));
            float reductionMultiplier = 1f - Mathf.Clamp01(reduction);

            minimumDuration = Math.Max(0f, MinimumDurationSeconds.Value);
            maximumDuration = Math.Max(minimumDuration, MaximumDurationSeconds.Value);
            additionalDuration = Mathf.Lerp(Math.Max(0f, MinimumDurationIncreasePerShotSeconds.Value), Math.Max(MinimumDurationIncreasePerShotSeconds.Value, MaximumDurationIncreasePerShotSeconds.Value), normalizedDistance) * reductionMultiplier;

            minimumIntensity = Math.Max(0f, MinimumIntensity.Value);
            maximumIntensity = Math.Max(minimumIntensity, MaximumIntensity.Value);
            additionalIntensity = Mathf.Lerp(Math.Max(0f, MinimumIntensityIncreasePerShot.Value), Math.Max(MinimumIntensityIncreasePerShot.Value, MaximumIntensityIncreasePerShot.Value), normalizedDistance) * reductionMultiplier;
        }

        internal static float CalculateProtectionReduction(bool hasHeadphones, float gunsCompressorSendLevel, int deafStrength, bool isSilenced, int suppressorLoudness)
        {
            float equipmentReduction;
            if (hasHeadphones)
                equipmentReduction = Mathf.Clamp01(Mathf.InverseLerp(GunsCompressorHighLevel.Value, GunsCompressorLowLevel.Value, gunsCompressorSendLevel));
            else if (deafStrength >= 2)
                equipmentReduction = Mathf.Clamp01(StrongMuteReduction.Value);
            else if (deafStrength == 1)
                equipmentReduction = Mathf.Clamp01(LowMuteReduction.Value);
            else
                equipmentReduction = 0f;

            if (!isSilenced)
                return equipmentReduction;

            float suppressorReduction = Mathf.Clamp01(Mathf.Max(0f, -suppressorLoudness) / 100f * SuppressorLoudnessReductionMultiplier.Value);
            return 1f - ((1f - equipmentReduction) * (1f - suppressorReduction));
        }

        internal static void CalculateConcussionFromProtection(string caliber, bool hasHeadphones, float gunsCompressorSendLevel, int deafStrength, bool isSilenced, int suppressorLoudness, out float audibleDistance, out float reduction, out float additionalDuration, out float additionalIntensity, out float minimumDuration, out float maximumDuration, out float minimumIntensity, out float maximumIntensity, out float maxDoubleVisionAmount, out float maxWiggleSpeed, out float maxWiggleScale, out float maxWiggleStrength, out float maxMotionBlurAmount)
        {
            if (!TryGetAudibleDistance(caliber, out audibleDistance))
            {
                reduction = 0f;
                additionalDuration = 0f;
                additionalIntensity = 0f;
                minimumDuration = 0f;
                maximumDuration = 0f;
                minimumIntensity = 0f;
                maximumIntensity = 0f;
            }
            else
            {
                reduction = CalculateProtectionReduction(hasHeadphones, gunsCompressorSendLevel, deafStrength, isSilenced, suppressorLoudness);
                CalculateConcussion(audibleDistance, reduction, out additionalDuration, out additionalIntensity, out minimumDuration, out maximumDuration, out minimumIntensity, out maximumIntensity);
            }

            maxDoubleVisionAmount = MaxDoubleVisionAmount;
            maxWiggleSpeed = MaxWiggleSpeed;
            maxWiggleScale = MaxWiggleScale;
            maxWiggleStrength = MaxWiggleStrength;
            maxMotionBlurAmount = MaxMotionBlurAmount;
        }

        internal static void ApplyHostVisualSettings(float maxDoubleVisionAmount, float maxWiggleSpeed, float maxWiggleScale, float maxWiggleStrength, float maxMotionBlurAmount)
        {
            MaxDoubleVisionAmount = Math.Max(0f, maxDoubleVisionAmount);
            MaxWiggleSpeed = Math.Max(0f, maxWiggleSpeed);
            MaxWiggleScale = Math.Max(0f, maxWiggleScale);
            MaxWiggleStrength = Math.Max(0f, maxWiggleStrength);
            MaxMotionBlurAmount = Math.Max(0f, maxMotionBlurAmount);
        }

        private static ConfigDescription CaliberDescription()
        {
            return new ConfigDescription(
                "Audible distance in meters for this caliber. 0 disables mounted shot concussion for this caliber.",
                new AcceptableValueRange<float>(0f, 5000f),
                new global::ConfigurationManagerAttributes { IsAdvanced = true, ShowRangeAsPercent = false });
        }

        private static void Rebuild()
        {
            ApplyHostVisualSettings(MaxDoubleVisionAmountEntry.Value, MaxWiggleSpeedEntry.Value, MaxWiggleScaleEntry.Value, MaxWiggleStrengthEntry.Value, MaxMotionBlurAmountEntry.Value);
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

        private static void OnSettingsChanged(object sender, EventArgs args)
        {
            Rebuild();
        }

        private static string NormalizeCaliber(string caliber)
        {
            caliber = caliber.Trim();
            return caliber.StartsWith("Caliber", StringComparison.OrdinalIgnoreCase)
                ? caliber.Substring("Caliber".Length)
                : caliber;
        }
    }
}
