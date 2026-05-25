using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Main.ClientClasses.HandsControllers;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace MountYourFriendsFika
{
    internal sealed class FikaMountedShotConcussionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(
                typeof(FikaClientFirearmController),
                nameof(FikaClientFirearmController.InitiateShot),
                new[] { typeof(IWeapon), typeof(AmmoItemClass), typeof(Vector3), typeof(Vector3), typeof(Vector3), typeof(int), typeof(float) });

        [PatchPostfix]
        private static void Postfix(Player.FirearmController __instance, AmmoItemClass ammo)
        {
            Player mountedPlayer = Traverse.Create(__instance).Field("_player").GetValue<Player>();
            if (mountedPlayer == null || ammo == null)
                return;

            CoopHandler coopHandler = GetActiveCoopHandler();
            if (coopHandler == null || !ReferenceEquals(mountedPlayer, coopHandler.MyPlayer) || !IsHumanPlayer(coopHandler, mountedPlayer))
                return;

            if (!TryGetSupportPlayer(mountedPlayer, out Player supportPlayer))
            {
                Log($"Shot concussion skipped: shooter={GetPlayerName(mountedPlayer)} is not tracked as mounted on a support. mountedState={mountedPlayer.MovementContext?.IsInMountedState.ToString() ?? "null"} caliber={ammo.Caliber}.");
                return;
            }

            if (!IsHumanPlayer(coopHandler, supportPlayer))
            {
                Log($"Shot concussion skipped: support={GetPlayerName(supportPlayer)} is not a Fika human player. shooter={GetPlayerName(mountedPlayer)} caliber={ammo.Caliber}.");
                return;
            }

            bool isSilenced = __instance.IsSilenced;
            int suppressorLoudness = GetSuppressorLoudness(__instance);
            GetEarProtectionInfo(supportPlayer, out bool hasHeadphones, out float gunsCompressorSendLevel, out int deafStrength, out string protectionSource);

            ActiveHealthController healthController = supportPlayer.ActiveHealthController;
            if (healthController == null)
            {
                if (MountYourFriendsFikaPlugin.SendConcussionRequest(mountedPlayer, supportPlayer, ammo.Caliber, hasHeadphones, gunsCompressorSendLevel, deafStrength, isSilenced, suppressorLoudness))
                {
                    Log($"Requested remote shot concussion: shooter={GetPlayerName(mountedPlayer)} support={GetPlayerName(supportPlayer)} healthControllerType={supportPlayer.HealthController?.GetType().FullName ?? "null"} caliber={ammo.Caliber} hasHeadphones={hasHeadphones} gunsCompressorSendLevel={gunsCompressorSendLevel} deafStrength={deafStrength} isSilenced={isSilenced} suppressorLoudness={suppressorLoudness} source={protectionSource}.");
                    return;
                }

                Log($"Shot concussion skipped: support={GetPlayerName(supportPlayer)} has no active health controller and no remote handler. healthControllerType={supportPlayer.HealthController?.GetType().FullName ?? "null"} isYourPlayer={supportPlayer.IsYourPlayer} isAI={supportPlayer.IsAI}.");
                return;
            }

            if (!MountShotConcussionSettings.TryGetAudibleDistance(ammo.Caliber, out float audibleDistance))
            {
                Log($"Shot concussion skipped: caliber={ammo.Caliber} is not whitelisted or resolved to zero strength.");
                return;
            }

            float reduction = MountShotConcussionSettings.CalculateProtectionReduction(hasHeadphones, gunsCompressorSendLevel, deafStrength, isSilenced, suppressorLoudness);
            MountShotConcussionSettings.CalculateConcussion(
                audibleDistance,
                reduction,
                out float additionalDuration,
                out float additionalIntensity,
                out float minimumDuration,
                out float maximumDuration,
                out float minimumIntensity,
                out float maximumIntensity);

            if (additionalDuration <= 0f || additionalIntensity <= 0f)
            {
                Log($"Shot concussion prevented by protection: shooter={GetPlayerName(mountedPlayer)} support={GetPlayerName(supportPlayer)} caliber={ammo.Caliber} audibleDistance={audibleDistance} reduction={reduction} isSilenced={isSilenced} suppressorLoudness={suppressorLoudness} source={protectionSource}.");
                return;
            }

            if (ApplyConcussion(supportPlayer, additionalDuration, additionalIntensity, minimumDuration, maximumDuration, minimumIntensity, maximumIntensity, out float appliedDuration, out float appliedIntensity))
            {
                Log($"Applying shot concussion: shooter={GetPlayerName(mountedPlayer)} support={GetPlayerName(supportPlayer)} caliber={ammo.Caliber} audibleDistance={audibleDistance} reduction={reduction} isSilenced={isSilenced} suppressorLoudness={suppressorLoudness} additionalDuration={additionalDuration} additionalIntensity={additionalIntensity} appliedDuration={appliedDuration} appliedIntensity={appliedIntensity} source={protectionSource}.");
                return;
            }

            Log($"Shot concussion skipped: support={GetPlayerName(supportPlayer)} additive concussion resolved to zero.");
        }

        private static bool ApplyConcussion(Player player, float additionalDuration, float additionalIntensity, float minimumDuration, float maximumDuration, float minimumIntensity, float maximumIntensity, out float appliedDuration, out float appliedIntensity)
        {
            appliedDuration = 0f;
            appliedIntensity = 0f;

            ActiveHealthController healthController = player?.ActiveHealthController;
            if (healthController == null)
                return false;

            IEffect contusion = healthController.FindActiveEffect<GInterface352>(EBodyPart.Head);
            if (contusion == null)
            {
                appliedDuration = Mathf.Clamp(additionalDuration, Mathf.Max(0f, minimumDuration), Mathf.Max(0f, maximumDuration));
                appliedIntensity = Mathf.Clamp(additionalIntensity, Mathf.Max(0f, minimumIntensity), Mathf.Max(0f, maximumIntensity));
                healthController.DoContusion(appliedDuration, appliedIntensity);
                StartTinnitus(player, appliedDuration);
                return true;
            }

            appliedDuration = Mathf.Clamp(contusion.TimeLeft + additionalDuration, Mathf.Max(0f, minimumDuration), Mathf.Max(0f, maximumDuration));
            appliedIntensity = Mathf.Clamp(contusion.Strength + additionalIntensity, Mathf.Max(0f, minimumIntensity), Mathf.Max(0f, maximumIntensity));

            MethodInfo addWorkTime = contusion.GetType().GetMethod("AddWorkTime", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setStrength = contusion.GetType().GetMethod("SetStrength", BindingFlags.Public | BindingFlags.Instance);
            if (addWorkTime == null || setStrength == null)
                return false;

            addWorkTime.Invoke(contusion, new object[] { (float?)appliedDuration, true });
            setStrength.Invoke(contusion, new object[] { appliedIntensity });
            StartTinnitus(player, appliedDuration);
            return true;
        }

        private static bool TryGetSupportPlayer(Player mountedPlayer, out Player supportPlayer)
        {
            supportPlayer = null;
            Type bridgeType = Type.GetType("MountYourFriends.MountSupportBridge, MountYourFriends");
            MethodInfo method = bridgeType?.GetMethod("TryGetSupportPlayer", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
                Log("Shot concussion skipped: MountSupportBridge.TryGetSupportPlayer was not found.");
                return false;
            }

            object[] parameters = { mountedPlayer, null };
            bool result = (bool)method.Invoke(null, parameters);
            supportPlayer = parameters[1] as Player;
            return result;
        }

        private static void GetEarProtectionInfo(Player player, out bool hasHeadphones, out float gunsCompressorSendLevel, out int deafStrength, out string protectionSource)
        {
            hasHeadphones = false;
            gunsCompressorSendLevel = 0f;
            deafStrength = 0;

            HeadphonesItemClass headphones = GetHeadphones(player);
            if (headphones != null)
            {
                hasHeadphones = true;
                gunsCompressorSendLevel = headphones.Template.GunsCompressorSendLevel;
                protectionSource = $"headphones={headphones.ShortName.Localized(null)}, gunsCompressorSendLevel={gunsCompressorSendLevel}";
                return;
            }

            EDeafStrength helmetDeafStrength = GetHelmetDeafStrength(player);
            deafStrength = (int)helmetDeafStrength;
            protectionSource = helmetDeafStrength == EDeafStrength.High
                ? "helmetDeafness=High"
                : helmetDeafStrength == EDeafStrength.Low
                    ? "helmetDeafness=Low"
                    : "none";
        }

        private static HeadphonesItemClass GetHeadphones(Player player)
        {
            Item earpiece = player.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            if (earpiece is HeadphonesItemClass headphones)
                return headphones;

            CompoundItem headwear = player.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem;
            return headwear?.GetAllItemsFromCollection().OfType<HeadphonesItemClass>().FirstOrDefault();
        }

        private static EDeafStrength GetHelmetDeafStrength(Player player)
        {
            EDeafStrength headwearDeaf = GetDeafStrength(player.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem as CompoundItem, true);
            EDeafStrength faceCoverDeaf = GetDeafStrength(player.Equipment.GetSlot(EquipmentSlot.FaceCover).ContainedItem as CompoundItem, false);
            return (EDeafStrength)Mathf.Max((int)headwearDeaf, (int)faceCoverDeaf);
        }

        private static int GetSuppressorLoudness(Player.FirearmController firearmController)
        {
            if (firearmController?.Item == null || !firearmController.IsSilenced)
                return 0;

            int loudness = 0;
            foreach (Mod mod in firearmController.Item.GetAllItemsFromCollection().OfType<Mod>())
            {
                if (mod.GetItemComponentsInChildren<SilencerComponent>(true).Any())
                    loudness = Math.Min(loudness, mod.Loudness);
            }

            return loudness;
        }

        private static EDeafStrength GetDeafStrength(CompoundItem item, bool includeTemplate)
        {
            if (item == null)
                return EDeafStrength.None;

            EDeafStrength deafStrength = EDeafStrength.None;
            foreach (CompositeArmorComponent armorComponent in item.GetItemComponentsInChildren<CompositeArmorComponent>(true))
            {
                if (armorComponent.Deaf > deafStrength)
                    deafStrength = armorComponent.Deaf;
            }

            if (includeTemplate && item.Template is ArmoredEquipmentTemplateClass armorTemplate && armorTemplate.DeafStrength > deafStrength)
                deafStrength = armorTemplate.DeafStrength;

            return deafStrength;
        }

        private static void StartTinnitus(Player player, float duration)
        {
            if (!Singleton<BetterAudio>.Instantiated)
                return;

            if (GetHeadphones(player) != null)
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
            if (!(player is FikaPlayer fikaPlayer) || coopHandler?.HumanPlayers == null)
                return false;

            for (int i = 0; i < coopHandler.HumanPlayers.Count; i++)
            {
                FikaPlayer humanPlayer = coopHandler.HumanPlayers[i];
                if (humanPlayer != null && humanPlayer.NetId == fikaPlayer.NetId)
                    return true;
            }

            return false;
        }

        private static string GetPlayerName(Player player)
        {
            return player?.Profile?.Info?.Nickname ?? "unknown";
        }

        private static void Log(string message)
        {
            if (MountShotConcussionSettings.DebugLogging.Value)
                MountYourFriendsFikaPlugin.LogSource.LogInfo(message);
        }
    }
}
