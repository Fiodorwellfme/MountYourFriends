using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using UnityEngine;

namespace MountYourFriendsFika
{
    [BepInPlugin("com.fiodor.mountyourfriendsfika", "Mount Your Friends Fika Sync", "1.0.0")]
    [BepInDependency("com.fiodor.MountYourFriends", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class MountYourFriendsFikaPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource LogSource;
        private static EventInfo RemoteOrbitUpdatedEvent;
        private static Delegate RemoteOrbitUpdatedHandler;
        private Harmony HarmonyInstance;

        private void Awake()
        {
            LogSource = Logger;
            MountShotConcussionSettings.Init(Config);
            SubscribeToBaseBridge();
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            new ContusionDoubleVisionIntensityPatch().Enable();
            new ContusionWiggleIntensityPatch().Enable();
            new ContusionMotionBlurIntensityPatch().Enable();
            new FikaMountedShotConcussionPatch().Enable();
            HarmonyInstance = new Harmony("com.fiodor.mountyourfriendsfika");
            HarmonyInstance.PatchAll();
            LogSource.LogInfo("Mount Your Friends Fika Sync 1.0.0 loaded.");
        }

        private void OnDestroy()
        {
            if (RemoteOrbitUpdatedEvent != null && RemoteOrbitUpdatedHandler != null)
                RemoteOrbitUpdatedEvent.RemoveEventHandler(null, RemoteOrbitUpdatedHandler);

            HarmonyInstance?.UnpatchSelf();
        }

        private static void SubscribeToBaseBridge()
        {
            Type orbitBridgeType = Type.GetType("MountYourFriends.MountBodyOrbitBridge, MountYourFriends");
            if (orbitBridgeType == null)
            {
                LogSource.LogError("Could not find MountBodyOrbitBridge in MountYourFriends.");
                return;
            }

            RemoteOrbitUpdatedEvent = orbitBridgeType.GetEvent("RemoteOrbitUpdated", BindingFlags.Public | BindingFlags.Static);
            MethodInfo orbitHandlerMethod = typeof(MountYourFriendsFikaPlugin).GetMethod(nameof(OnRemoteOrbitUpdated), BindingFlags.NonPublic | BindingFlags.Static);
            if (RemoteOrbitUpdatedEvent == null || orbitHandlerMethod == null)
            {
                LogSource.LogError("Could not bind MountBodyOrbitBridge.RemoteOrbitUpdated.");
                return;
            }

            RemoteOrbitUpdatedHandler = Delegate.CreateDelegate(RemoteOrbitUpdatedEvent.EventHandlerType, orbitHandlerMethod);
            RemoteOrbitUpdatedEvent.AddEventHandler(null, RemoteOrbitUpdatedHandler);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent createdEvent)
        {
            if (createdEvent.Manager is FikaServer fikaServer)
            {
                fikaServer.RegisterPacket<MountConcussionPacket, NetPeer>(OnServerConcussionPacketReceived);
                fikaServer.RegisterPacket<MountBodyOrbitPacket, NetPeer>(OnServerBodyOrbitPacketReceived);
                return;
            }

            if (createdEvent.Manager is FikaClient fikaClient)
            {
                fikaClient.RegisterPacket<MountConcussionPacket>(OnClientConcussionPacketReceived);
                fikaClient.RegisterPacket<MountBodyOrbitPacket>(OnClientBodyOrbitPacketReceived);
                return;
            }

            LogSource.LogWarning($"Unsupported Fika network manager type: {createdEvent.Manager.GetType().FullName}.");
        }

        internal static bool SendConcussionRequest(Player sourcePlayer, Player targetPlayer, string caliber, bool hasHeadphones, float gunsCompressorSendLevel, int deafStrength, bool isSilenced, int suppressorLoudness)
        {
            CoopHandler coopHandler = GetActiveCoopHandler();
            if (coopHandler == null || !ReferenceEquals(sourcePlayer, coopHandler.MyPlayer) || !IsHumanPlayer(coopHandler, sourcePlayer) || !IsHumanPlayer(coopHandler, targetPlayer))
                return false;

            if (!(targetPlayer is FikaPlayer targetFikaPlayer))
            {
                LogSource.LogWarning($"Cannot request remote concussion for non-Fika player {GetPlayerName(targetPlayer)}.");
                return false;
            }

            MountConcussionPacket packet = new MountConcussionPacket
            {
                TargetNetId = targetFikaPlayer.NetId,
                Caliber = caliber,
                HasHeadphones = hasHeadphones ? (byte)1 : (byte)0,
                GunsCompressorSendLevel = gunsCompressorSendLevel,
                DeafStrength = deafStrength,
                IsSilenced = isSilenced ? (byte)1 : (byte)0,
                SuppressorLoudness = suppressorLoudness
            };

            if (Singleton<FikaServer>.Instantiated)
            {
                if (!TryComputeWithHostSettings(ref packet))
                    return false;

                Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                LogSource.LogInfo($"Sent host concussion request: target={GetPlayerName(targetPlayer)} netId={packet.TargetNetId} additionalDuration={packet.AdditionalDuration} additionalIntensity={packet.AdditionalIntensity}.");
                return true;
            }

            if (Singleton<FikaClient>.Instantiated)
            {
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, true);
                return true;
            }

            LogSource.LogWarning($"Cannot send concussion request for {GetPlayerName(targetPlayer)}: no Fika network singleton is active.");
            return false;
        }

        private static void OnRemoteOrbitUpdated(Player player, Vector3 targetPosition, Quaternion targetBodyRotation, float yaw)
        {
            CoopHandler coopHandler = GetActiveCoopHandler();
            if (coopHandler == null || !ReferenceEquals(player, coopHandler.MyPlayer) || !IsHumanPlayer(coopHandler, player))
                return;

            if (!(coopHandler.MyPlayer is FikaPlayer fikaPlayer))
                return;

            MountBodyOrbitPacket packet = new MountBodyOrbitPacket
            {
                NetId = fikaPlayer.NetId,
                TargetPosition = targetPosition,
                TargetBodyRotation = targetBodyRotation,
                Yaw = yaw
            };

            if (Singleton<FikaServer>.Instantiated)
            {
                Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.Unreliable, true);
                return;
            }

            if (Singleton<FikaClient>.Instantiated)
                Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.Unreliable, true);
        }

        private static void OnServerConcussionPacketReceived(MountConcussionPacket packet, NetPeer peer)
        {
            LogSource.LogInfo($"Received concussion request on server: targetNetId={packet.TargetNetId} isComputed={packet.IsComputed} caliber={packet.Caliber} hasHeadphones={packet.HasHeadphones} gunsCompressorSendLevel={packet.GunsCompressorSendLevel} deafStrength={packet.DeafStrength} isSilenced={packet.IsSilenced} suppressorLoudness={packet.SuppressorLoudness}.");

            if (packet.IsComputed == 0 && !TryComputeWithHostSettings(ref packet))
                return;

            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered, peer);
            LogSource.LogInfo($"Forwarded concussion request from server: targetNetId={packet.TargetNetId} additionalDuration={packet.AdditionalDuration} additionalIntensity={packet.AdditionalIntensity}.");
        }

        private static void OnClientConcussionPacketReceived(MountConcussionPacket packet)
        {
            ApplyIfLocalOwner(packet);
        }

        private static void OnServerBodyOrbitPacketReceived(MountBodyOrbitPacket packet, NetPeer peer)
        {
            Singleton<FikaServer>.Instance.SendData(ref packet, DeliveryMethod.Unreliable, peer);
        }

        private static void OnClientBodyOrbitPacketReceived(MountBodyOrbitPacket packet)
        {
            ApplyBodyOrbit(packet);
        }

        private static void ApplyBodyOrbit(MountBodyOrbitPacket packet)
        {
            CoopHandler coopHandler = GetActiveCoopHandler();
            FikaPlayer player = ResolveHumanPlayer(coopHandler, packet.NetId);
            if (player == null || ReferenceEquals(player, coopHandler.MyPlayer) || player.IsYourPlayer || !player.MovementContext.IsInMountedState)
                return;

            player.MovementContext.PlayerMountingPointData.PlayerTargetPos = packet.TargetPosition;
            player.MovementContext.PlayerMountingPointData.TargetBodyRotation = packet.TargetBodyRotation;
            player.MovementContext.TransformPosition = packet.TargetPosition;
            player.Rotation = new Vector2(packet.Yaw, player.Rotation.y);
            player.MovementContext.ApplyRotation(packet.TargetBodyRotation);
            player.MovementContext.UpdateDeltaAngle();
        }

        private static void ApplyIfLocalOwner(MountConcussionPacket packet)
        {
            if (packet.IsComputed == 0)
                return;

            ApplyHostVisualSettings(packet);

            if (packet.AdditionalDuration <= 0f || packet.AdditionalIntensity <= 0f)
            {
                LogSource.LogInfo($"Remote concussion ignored: host computed zero effect for targetNetId={packet.TargetNetId} reduction={packet.Reduction} additionalDuration={packet.AdditionalDuration} additionalIntensity={packet.AdditionalIntensity}.");
                return;
            }

            Player player = ResolveLocalPlayer(packet.TargetNetId);
            if (player == null)
            {
                LogSource.LogInfo($"Remote concussion ignored: targetNetId={packet.TargetNetId} is not owned by this client.");
                return;
            }

            if (!ApplyConcussion(player, packet, out float appliedDuration, out float appliedIntensity))
            {
                LogSource.LogWarning($"Cannot apply remote concussion: local target={GetPlayerName(player)} netId={packet.TargetNetId} healthControllerType={player.HealthController?.GetType().FullName ?? "null"} activeHealthController={(player.ActiveHealthController == null ? "null" : player.ActiveHealthController.GetType().FullName)}.");
                return;
            }

            LogSource.LogInfo($"Applied remote concussion: target={GetPlayerName(player)} netId={packet.TargetNetId} additionalDuration={packet.AdditionalDuration} additionalIntensity={packet.AdditionalIntensity} appliedDuration={appliedDuration} appliedIntensity={appliedIntensity}.");
        }

        private static bool TryComputeWithHostSettings(ref MountConcussionPacket packet)
        {
            MountShotConcussionSettings.CalculateConcussionFromProtection(
                packet.Caliber,
                packet.HasHeadphones != 0,
                packet.GunsCompressorSendLevel,
                packet.DeafStrength,
                packet.IsSilenced != 0,
                packet.SuppressorLoudness,
                out packet.AudibleDistance,
                out packet.Reduction,
                out packet.AdditionalDuration,
                out packet.AdditionalIntensity,
                out packet.MinimumDuration,
                out packet.MaximumDuration,
                out packet.MinimumIntensity,
                out packet.MaximumIntensity,
                out packet.MaxDoubleVisionAmount,
                out packet.MaxWiggleSpeed,
                out packet.MaxWiggleScale,
                out packet.MaxWiggleStrength,
                out packet.MaxMotionBlurAmount);
            packet.IsComputed = 1;
            return true;
        }

        private static void ApplyHostVisualSettings(MountConcussionPacket packet)
        {
            MountShotConcussionSettings.ApplyHostVisualSettings(
                packet.MaxDoubleVisionAmount,
                packet.MaxWiggleSpeed,
                packet.MaxWiggleScale,
                packet.MaxWiggleStrength,
                packet.MaxMotionBlurAmount);
        }

        private static bool ApplyConcussion(Player player, MountConcussionPacket packet, out float appliedDuration, out float appliedIntensity)
        {
            appliedDuration = 0f;
            appliedIntensity = 0f;

            ActiveHealthController healthController = player?.ActiveHealthController;
            if (healthController == null)
                return false;

            IEffect contusion = healthController.FindActiveEffect<GInterface352>(EBodyPart.Head);
            if (contusion == null)
            {
                appliedDuration = Mathf.Clamp(packet.AdditionalDuration, Mathf.Max(0f, packet.MinimumDuration), Mathf.Max(0f, packet.MaximumDuration));
                appliedIntensity = Mathf.Clamp(packet.AdditionalIntensity, Mathf.Max(0f, packet.MinimumIntensity), Mathf.Max(0f, packet.MaximumIntensity));
                healthController.DoContusion(appliedDuration, appliedIntensity);
                StartTinnitus(player, appliedDuration);
                return true;
            }

            appliedDuration = Mathf.Clamp(contusion.TimeLeft + packet.AdditionalDuration, Mathf.Max(0f, packet.MinimumDuration), Mathf.Max(0f, packet.MaximumDuration));
            appliedIntensity = Mathf.Clamp(contusion.Strength + packet.AdditionalIntensity, Mathf.Max(0f, packet.MinimumIntensity), Mathf.Max(0f, packet.MaximumIntensity));

            MethodInfo addWorkTime = contusion.GetType().GetMethod("AddWorkTime", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setStrength = contusion.GetType().GetMethod("SetStrength", BindingFlags.Public | BindingFlags.Instance);
            if (addWorkTime == null || setStrength == null)
                return false;

            addWorkTime.Invoke(contusion, new object[] { (float?)appliedDuration, true });
            setStrength.Invoke(contusion, new object[] { appliedIntensity });
            StartTinnitus(player, appliedDuration);
            return true;
        }

        private static void StartTinnitus(Player player, float duration)
        {
            if (!Singleton<BetterAudio>.Instantiated)
                return;

            if (HasHeadphones(player))
            {
                SuppressTinnitus();
                return;
            }

            player.method_88(duration / 2f);
        }

        private static void SuppressTinnitus()
        {
            FieldInfo tinnitusEndTime = typeof(BetterAudio).GetField("float_2", BindingFlags.NonPublic | BindingFlags.Instance);
            tinnitusEndTime?.SetValue(Singleton<BetterAudio>.Instance, Time.time);
        }

        private static bool HasHeadphones(Player player)
        {
            Item earpiece = player.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            if (earpiece is HeadphonesItemClass)
                return true;

            CompoundItem headwear = player.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem;
            return headwear?.GetAllItemsFromCollection().OfType<HeadphonesItemClass>().Any() == true;
        }

        private static Player ResolveLocalPlayer(int netId)
        {
            if (Singleton<FikaClient>.Instantiated)
                return ResolveLocalPlayer(Singleton<FikaClient>.Instance.CoopHandler, netId);

            if (Singleton<FikaServer>.Instantiated)
                return ResolveLocalPlayer(Singleton<FikaServer>.Instance.CoopHandler, netId);

            return null;
        }

        private static Player ResolveLocalPlayer(CoopHandler coopHandler, int netId)
        {
            FikaPlayer humanPlayer = ResolveHumanPlayer(coopHandler, netId);
            if (humanPlayer != null && humanPlayer.IsYourPlayer)
                return humanPlayer;

            return null;
        }

        private static CoopHandler GetActiveCoopHandler()
        {
            if (Singleton<FikaClient>.Instantiated)
                return Singleton<FikaClient>.Instance.CoopHandler;

            if (Singleton<FikaServer>.Instantiated)
                return Singleton<FikaServer>.Instance.CoopHandler;

            return null;
        }

        private static bool IsHumanPlayer(CoopHandler coopHandler, Player player)
        {
            if (!(player is FikaPlayer fikaPlayer))
                return false;

            return ResolveHumanPlayer(coopHandler, fikaPlayer.NetId) != null;
        }

        private static FikaPlayer ResolveHumanPlayer(CoopHandler coopHandler, int netId)
        {
            if (coopHandler == null || coopHandler.HumanPlayers == null)
                return null;

            for (int i = 0; i < coopHandler.HumanPlayers.Count; i++)
            {
                FikaPlayer humanPlayer = coopHandler.HumanPlayers[i];
                if (humanPlayer != null && humanPlayer.NetId == netId)
                    return humanPlayer;
            }

            return null;
        }

        private static string GetPlayerName(Player player)
        {
            return player?.Profile?.Info?.Nickname ?? "unknown";
        }
    }
}
