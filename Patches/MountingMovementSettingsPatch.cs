using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MountYourFriends.Patches
{
    internal sealed class MountingMovementSettingsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(GClass2667), "method_4", new[] { typeof(MountPointData) });

        [PatchPrefix]
        private static void Prefix(GClass2667 __instance)
        {
            MountingSettingsOverrides.ApplyMovementSettings(__instance.MountingMovementSettings);
        }

        [PatchTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            MethodInfo atanMethod = AccessTools.Method(typeof(Mathf), nameof(Mathf.Atan), new[] { typeof(float) });
            MethodInfo extraYawMethod = AccessTools.Method(typeof(MountingSettingsOverrides), nameof(MountingSettingsOverrides.GetExtraYawDegrees));

            for (int i = 0; i < codes.Count; i++)
            {
                if (!codes[i].Calls(atanMethod))
                {
                    continue;
                }

                for (int j = i + 1; j < codes.Count && j < i + 8; j++)
                {
                    if (codes[j].opcode != OpCodes.Sub)
                    {
                        continue;
                    }

                    codes.Insert(j + 1, new CodeInstruction(OpCodes.Call, extraYawMethod));
                    codes.Insert(j + 2, new CodeInstruction(OpCodes.Add));
                    return codes;
                }
            }

            return codes;
        }
    }
}
