using EFT;

namespace MountYourFriends
{
    public static class MountSupportBridge
    {
        public static bool TryGetSupportPlayer(Player mountedPlayer, out Player supportPlayer)
        {
            return SupportMountTracker.TryGetSupportPlayer(mountedPlayer, out supportPlayer);
        }
    }
}
