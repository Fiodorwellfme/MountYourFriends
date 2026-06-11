using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.WeaponMounting;
using UnityEngine;

namespace MountYourFriends
{
    internal static class SupportMountTracker
    {
        private static readonly Collider[] PlayerColliders = new Collider[16];
        private static readonly Dictionary<MovementContext, SupportMountState> ActiveSupports = new Dictionary<MovementContext, SupportMountState>();
        private static readonly List<MovementContext> ContextsToRemove = new List<MovementContext>();

        internal static void TryCapture(GClass2667 mountingComponent, MountPointData mountPointData)
        {
            MovementContext mountedContext = mountingComponent.MovementContext_0;
            ActiveSupports.Remove(mountedContext);

            if (!mountedContext.IsInMountedState)
            {
                Log("Support mount capture skipped: movement context is not in mounted state.");
                return;
            }

            Player supportPlayer = FindSupportPlayer(mountedContext, mountPointData.MountPoint);
            if (supportPlayer == null)
            {
                Log($"Support mount capture skipped: no support player found within {Settings.SupportDetectionRadiusMeters.Value}m of mount point.");
                return;
            }

            ActiveSupports[mountedContext] = new SupportMountState(
                supportPlayer,
                supportPlayer.MovementContext.TransformPosition,
                supportPlayer.MovementContext.Yaw);

            Log($"Captured support mount: support={GetPlayerName(supportPlayer)}.");
        }

        internal static void Tick(float deltaTime)
        {
            if (ActiveSupports.Count == 0)
                return;

            ContextsToRemove.Clear();

            foreach (KeyValuePair<MovementContext, SupportMountState> pair in ActiveSupports)
            {
                MovementContext mountedContext = pair.Key;
                SupportMountState state = pair.Value;

                if (!mountedContext.IsInMountedState || state.SupportPlayer == null || state.SupportPlayer.MovementContext == null)
                {
                    ContextsToRemove.Add(mountedContext);
                    continue;
                }

                state.TimeUntilNextCheck -= deltaTime;
                if (state.TimeUntilNextCheck > 0f)
                    continue;

                state.TimeUntilNextCheck = Mathf.Max(0.01f, Settings.SupportMoveCheckIntervalSeconds.Value);

                if (SupportMovedTooFar(state) || SupportRotatedTooFar(state))
                {
                    mountedContext.StartExitingMountedState();
                    ContextsToRemove.Add(mountedContext);
                }
            }

            for (int i = 0; i < ContextsToRemove.Count; i++)
                ActiveSupports.Remove(ContextsToRemove[i]);
        }

        internal static bool TryGetSupportPlayer(Player mountedPlayer, out Player supportPlayer)
        {
            supportPlayer = null;

            if (mountedPlayer == null || mountedPlayer.MovementContext == null)
                return false;

            if (!ActiveSupports.TryGetValue(mountedPlayer.MovementContext, out SupportMountState state))
                return false;

            supportPlayer = state.SupportPlayer;
            return supportPlayer != null;
        }

        private static Player FindSupportPlayer(MovementContext mountedContext, Vector3 mountPoint)
        {
            int count = Physics.OverlapSphereNonAlloc(
                mountPoint,
                Mathf.Max(0.01f, Settings.SupportDetectionRadiusMeters.Value),
                PlayerColliders,
                LayerMaskClass.PlayerMask,
                QueryTriggerInteraction.Collide);

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            for (int i = 0; i < count; i++)
            {
                Player player = gameWorld.GetPlayerByCollider(PlayerColliders[i]);
                if (player != null && player.MovementContext != mountedContext)
                    return player;
            }

            return null;
        }

        private static bool SupportMovedTooFar(SupportMountState state)
        {
            float threshold = Settings.SupportMoveUnmountThresholdMeters.Value;
            if (threshold <= 0f)
                return false;

            return (state.SupportPlayer.MovementContext.TransformPosition - state.InitialPosition).sqrMagnitude > threshold * threshold;
        }

        private static bool SupportRotatedTooFar(SupportMountState state)
        {
            float threshold = Settings.SupportYawUnmountThresholdDegrees.Value;
            if (threshold <= 0f)
                return false;

            return Mathf.Abs(Mathf.DeltaAngle(state.InitialYaw, state.SupportPlayer.MovementContext.Yaw)) > threshold;
        }

        private static string GetPlayerName(Player player)
        {
            return player?.Profile?.Info?.Nickname ?? "unknown";
        }

        private static void Log(string message)
        {
            if (Settings.DebugLogging.Value)
                global::MountYourFriends.MountYourFriends.LogSource.LogInfo(message);
        }

        private sealed class SupportMountState
        {
            internal readonly Player SupportPlayer;
            internal readonly Vector3 InitialPosition;
            internal readonly float InitialYaw;
            internal float TimeUntilNextCheck;

            internal SupportMountState(Player supportPlayer, Vector3 initialPosition, float initialYaw)
            {
                SupportPlayer = supportPlayer;
                InitialPosition = initialPosition;
                InitialYaw = initialYaw;
            }
        }
    }
}
