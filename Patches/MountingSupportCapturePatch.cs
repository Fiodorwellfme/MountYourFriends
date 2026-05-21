using System.Reflection;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MountYourFriends.Patches
{
    internal sealed class MountingSupportCapturePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(GClass2667), "method_1", new[] { typeof(MountPointData) });

        [PatchPostfix]
        private static void Postfix(GClass2667 __instance, MountPointData mountPointData)
        {
            SupportMountTracker.TryCapture(__instance, mountPointData);
        }
    }
}
