using Fika.Core.Networking.LiteNetLib.Utils;

namespace MountYourFriendsFika
{
    public struct MountConcussionPacket : INetSerializable
    {
        public int TargetNetId;
        public byte IsComputed;
        public string Caliber;
        public float AudibleDistance;
        public byte HasHeadphones;
        public float GunsCompressorSendLevel;
        public int DeafStrength;
        public byte IsSilenced;
        public int SuppressorLoudness;
        public float Reduction;
        public float AdditionalDuration;
        public float AdditionalIntensity;
        public float MinimumDuration;
        public float MaximumDuration;
        public float MinimumIntensity;
        public float MaximumIntensity;
        public float MaxDoubleVisionAmount;
        public float MaxWiggleSpeed;
        public float MaxWiggleScale;
        public float MaxWiggleStrength;
        public float MaxMotionBlurAmount;

        public void Deserialize(NetDataReader reader)
        {
            TargetNetId = reader.GetInt();
            IsComputed = reader.GetByte();
            Caliber = reader.GetString();
            AudibleDistance = reader.GetFloat();
            HasHeadphones = reader.GetByte();
            GunsCompressorSendLevel = reader.GetFloat();
            DeafStrength = reader.GetInt();
            IsSilenced = reader.GetByte();
            SuppressorLoudness = reader.GetInt();
            Reduction = reader.GetFloat();
            AdditionalDuration = reader.GetFloat();
            AdditionalIntensity = reader.GetFloat();
            MinimumDuration = reader.GetFloat();
            MaximumDuration = reader.GetFloat();
            MinimumIntensity = reader.GetFloat();
            MaximumIntensity = reader.GetFloat();
            MaxDoubleVisionAmount = reader.GetFloat();
            MaxWiggleSpeed = reader.GetFloat();
            MaxWiggleScale = reader.GetFloat();
            MaxWiggleStrength = reader.GetFloat();
            MaxMotionBlurAmount = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(TargetNetId);
            writer.Put(IsComputed);
            writer.Put(Caliber ?? string.Empty);
            writer.Put(AudibleDistance);
            writer.Put(HasHeadphones);
            writer.Put(GunsCompressorSendLevel);
            writer.Put(DeafStrength);
            writer.Put(IsSilenced);
            writer.Put(SuppressorLoudness);
            writer.Put(Reduction);
            writer.Put(AdditionalDuration);
            writer.Put(AdditionalIntensity);
            writer.Put(MinimumDuration);
            writer.Put(MaximumDuration);
            writer.Put(MinimumIntensity);
            writer.Put(MaximumIntensity);
            writer.Put(MaxDoubleVisionAmount);
            writer.Put(MaxWiggleSpeed);
            writer.Put(MaxWiggleScale);
            writer.Put(MaxWiggleStrength);
            writer.Put(MaxMotionBlurAmount);
        }
    }
}
