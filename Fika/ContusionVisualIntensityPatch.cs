using System.Reflection;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MountYourFriendsFika
{
    internal sealed class ContusionDoubleVisionIntensityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(EffectsController.Class635), nameof(EffectsController.Class635.UpdateAmount));

        [PatchPrefix]
        private static bool Prefix(EffectsController.Class635 __instance)
        {
            if (!ContusionVisualDecay.TryGetRemainingFraction(__instance, out float remainingFraction))
                return true;

            float amount = Mathf.Lerp(0f, __instance.vmethod_0() * __instance.MaxEffectValue, remainingFraction);
            __instance.Cc_DoubleVision_0.amount = Cap(amount, MountShotConcussionSettings.MaxDoubleVisionAmount);
            return false;
        }

        private static float Cap(float value, float maximum)
            => Mathf.Min(value, Mathf.Max(0f, maximum));
    }

    internal sealed class ContusionWiggleIntensityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(EffectsController.Class645), nameof(EffectsController.Class645.UpdateAmount));

        [PatchPrefix]
        private static bool Prefix(EffectsController.Class645 __instance)
        {
            if (!ContusionVisualDecay.TryGetRemainingFraction(__instance, out float remainingFraction))
                return true;

            float value = Mathf.Lerp(0f, __instance.vmethod_0(), remainingFraction);
            __instance.Cc_Wiggle_0.speed = Cap(value * __instance.Float_0, MountShotConcussionSettings.MaxWiggleSpeed);
            __instance.Cc_Wiggle_0.scale = Cap(value * __instance.Float_1, MountShotConcussionSettings.MaxWiggleScale);
            __instance.Cc_Wiggle_0.str = Cap(value, MountShotConcussionSettings.MaxWiggleStrength);
            if (__instance.ActiveEffects.Count + __instance.List_0.Count == 0)
            {
                __instance.Cc_Wiggle_0.str = Mathf.Lerp(__instance.Cc_Wiggle_0.str, 0f, Time.deltaTime);
                if (__instance.Cc_Wiggle_0.str < 0.01f)
                    __instance.Toggle(false);
            }

            return false;
        }

        private static float Cap(float value, float maximum)
            => Mathf.Min(value, Mathf.Max(0f, maximum));
    }

    internal sealed class ContusionMotionBlurIntensityPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(EffectsController.Class646), nameof(EffectsController.Class646.UpdateAmount));

        [PatchPrefix]
        private static bool Prefix(EffectsController.Class646 __instance)
        {
            if (!ContusionVisualDecay.TryGetRemainingFraction(__instance, out float remainingFraction))
                return true;

            float value = Mathf.Lerp(0f, __instance.vmethod_0(), remainingFraction);
            __instance.MaxEffectValue = Mathf.Lerp(__instance.MaxEffectValue, __instance.Float_0, Time.deltaTime);
            __instance.MotionBlur_0.blurAmount = Cap(value * __instance.MaxEffectValue, MountShotConcussionSettings.MaxMotionBlurAmount);
            return false;
        }

        private static float Cap(float value, float maximum)
            => Mathf.Min(value, Mathf.Max(0f, maximum));
    }

    internal static class ContusionVisualDecay
    {
        public static bool TryGetRemainingFraction(EffectsController.Class633 controller, out float remainingFraction)
        {
            remainingFraction = 0f;
            bool hasContusion = false;

            foreach (IEffect effect in controller.ActiveEffects)
            {
                if (!(effect is GInterface352))
                    continue;

                hasContusion = true;
                if (effect.Residual)
                    continue;

                float duration = effect.WorkStateTime;
                if (duration <= 0f)
                    continue;

                remainingFraction = Mathf.Max(remainingFraction, Mathf.Clamp01(effect.TimeLeft / duration));
            }

            return hasContusion;
        }
    }
}
