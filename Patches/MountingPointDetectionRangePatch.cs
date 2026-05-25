using System.Reflection;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MountYourFriends.Patches
{
    internal sealed class MountingPointDetectionRangePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(GClass2666), "FindMountingPoint", new[] { typeof(float), typeof(float), typeof(float), typeof(float), typeof(bool) });

        [PatchPrefix]
        private static void Prefix(GClass2666 __instance)
        {
            MountingSettingsOverrides.ApplyDetectionSettings(__instance.ImountingPointDetectionSettings_0);
        }
    }
}
