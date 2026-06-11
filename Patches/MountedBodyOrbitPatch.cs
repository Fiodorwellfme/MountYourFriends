using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.WeaponMounting;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MountYourFriends.Patches
{
    internal sealed class MountedBodyOrbitEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(IdleWeaponMountingStateClass), nameof(IdleWeaponMountingStateClass.Enter));

        [PatchPostfix]
        private static void Postfix(IdleWeaponMountingStateClass __instance)
        {
            MountedBodyOrbitTracker.Capture(__instance);
        }
    }

    internal sealed class MountedBodyOrbitRotatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(IdleWeaponMountingStateClass), nameof(IdleWeaponMountingStateClass.Rotate));

        [PatchPrefix]
        private static void Prefix(IdleWeaponMountingStateClass __instance, out MountedBodyOrbitTracker.RotationSnapshot __state)
        {
            __state = MountedBodyOrbitTracker.CaptureRotation(__instance);
        }

        [PatchPostfix]
        private static void Postfix(IdleWeaponMountingStateClass __instance, MountedBodyOrbitTracker.RotationSnapshot __state)
        {
            MountedBodyOrbitTracker.Update(__instance, __state);
        }
    }

    internal static class MountedBodyOrbitTracker
    {
        private static readonly Dictionary<IdleWeaponMountingStateClass, OrbitState> OrbitStates = new Dictionary<IdleWeaponMountingStateClass, OrbitState>();
        private static readonly RaycastHit[] Hits = new RaycastHit[1];

        internal static RotationSnapshot CaptureRotation(IdleWeaponMountingStateClass state)
        {
            return new RotationSnapshot
            {
                Active = OrbitStates.ContainsKey(state),
                Rotation = state.MovementContext.Rotation,
                MountedYawDelta = state.Float_10
            };
        }

        internal static void Capture(IdleWeaponMountingStateClass state)
        {
            if (state.Player_0 == null || state.Player_0.IsAI || !state.Player_0.IsYourPlayer)
            {
                OrbitStates.Remove(state);
                return;
            }

            if (Settings.MountedExtraYawDegrees.Value <= 0f)
            {
                OrbitStates.Remove(state);
                return;
            }

            PlayerMountingPointData mountingData = state.PlayerMountingPointData_0;
            if (mountingData?.MountPointData == null)
            {
                OrbitStates.Remove(state);
                return;
            }

            Quaternion initialRotation = Quaternion.Euler(0f, mountingData.TargetHandsRotation, 0f);
            Vector3 pivotToBody = mountingData.PlayerTargetPos - mountingData.MountPointData.MountPoint;

            OrbitStates[state] = new OrbitState
            {
                Pivot = mountingData.MountPointData.MountPoint,
                LocalPivotToBody = Quaternion.Inverse(initialRotation) * pivotToBody
            };
        }

        internal static void Update(IdleWeaponMountingStateClass state, RotationSnapshot rotationSnapshot)
        {
            if (!OrbitStates.TryGetValue(state, out OrbitState orbitState))
            {
                return;
            }

            PlayerMountingPointData mountingData = state.PlayerMountingPointData_0;
            if (mountingData?.MountPointData == null || state.Bool_1 || state.Float_4 <= mountingData.CurrentApproachTime)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.Euler(0f, state.MovementContext.Yaw, 0f);
            Vector3 targetPosition = orbitState.Pivot + targetRotation * orbitState.LocalPivotToBody;
            targetPosition.y = mountingData.PlayerTargetPos.y;

            if (!CanMoveTo(state.MovementContext, targetPosition, targetRotation))
            {
                if (rotationSnapshot.Active)
                {
                    state.Float_10 = rotationSnapshot.MountedYawDelta;
                    state.MovementContext.Rotation = rotationSnapshot.Rotation;
                    state.MovementContext.UpdateDeltaAngle();
                }

                return;
            }

            mountingData.PlayerTargetPos = targetPosition;
            mountingData.TargetBodyRotation = targetRotation;
            state.MovementContext.TransformPosition = targetPosition;
            state.Player_0.Rotation = new Vector2(targetRotation.eulerAngles.y, state.Player_0.Rotation.y);
            state.MovementContext.ApplyRotation(targetRotation);
            state.MovementContext.UpdateDeltaAngle();
            MountBodyOrbitBridge.TrySendRemoteOrbit(state.Player_0, targetPosition, targetRotation, targetRotation.eulerAngles.y);
        }

        private static bool CanMoveTo(MovementContext movementContext, Vector3 targetPosition, Quaternion targetRotation)
        {
            Vector3 currentPosition = movementContext.TransformPosition;
            Vector3 motion = targetPosition - currentPosition;
            motion.y = 0f;
            if (motion.sqrMagnitude <= 0.000001f)
            {
                return true;
            }

            ICharacterController characterController = movementContext.CharacterController;
            float stepOffset = characterController.stepOffset;
            float width = characterController.radius + characterController.skinWidth;
            Vector3 center = currentPosition + Vector3.up * stepOffset + characterController.center;
            float halfWidth = Mathf.Sqrt(width / 2f) / 2f;
            Vector3 halfExtents = new Vector3(halfWidth, (characterController.height - stepOffset / 2f) / 2f, halfWidth);
            float maxDistance = motion.magnitude + width / 4f;

            return Physics.BoxCastNonAlloc(center, halfExtents, motion.normalized, Hits, targetRotation, maxDistance, LayerMaskClass.PlayerStaticCollisionsMask) == 0;
        }

        private struct OrbitState
        {
            internal Vector3 Pivot;
            internal Vector3 LocalPivotToBody;
        }

        internal struct RotationSnapshot
        {
            internal bool Active;
            internal Vector2 Rotation;
            internal float MountedYawDelta;
        }
    }
}
