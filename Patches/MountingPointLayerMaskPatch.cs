using System.Reflection;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace MountYourFriends.Patches
{
    internal sealed class MountingPointLayerMaskPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Constructor(typeof(GClass2666), new[] { typeof(IMountingPointDetectionSettings), typeof(WeaponMountingView) });

        [PatchPostfix]
        private static void Postfix(GClass2666 __instance)
        {
            __instance.LayerMask_0 |= LayerMaskClass.PlayerMask;
            __instance.LayerMask_1 |= LayerMaskClass.PlayerMask;
        }
    }
}
