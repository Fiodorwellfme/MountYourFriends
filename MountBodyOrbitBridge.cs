using System;
using EFT;
using UnityEngine;

namespace MountYourFriends
{
    public static class MountBodyOrbitBridge
    {
        public static event Action<Player, Vector3, Quaternion, float> RemoteOrbitUpdated;

        public static bool TrySendRemoteOrbit(Player player, Vector3 targetPosition, Quaternion targetBodyRotation, float yaw)
        {
            Action<Player, Vector3, Quaternion, float> handlers = RemoteOrbitUpdated;
            if (handlers == null)
                return false;

            bool handled = false;
            foreach (Delegate registeredHandler in handlers.GetInvocationList())
            {
                try
                {
                    Action<Player, Vector3, Quaternion, float> handler = (Action<Player, Vector3, Quaternion, float>)registeredHandler;
                    handler(player, targetPosition, targetBodyRotation, yaw);
                    handled = true;
                }
                catch (Exception exception)
                {
                    MountYourFriends.LogSource.LogError($"Remote mounted orbit handler failed: {exception}");
                }
            }

            return handled;
        }
    }
}
