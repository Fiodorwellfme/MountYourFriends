using Fika.Core.Networking.LiteNetLib.Utils;
using UnityEngine;

namespace MountYourFriendsFika
{
    public struct MountBodyOrbitPacket : INetSerializable
    {
        public int NetId;
        public Vector3 TargetPosition;
        public Quaternion TargetBodyRotation;
        public float Yaw;

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            TargetPosition = reader.GetUnmanaged<Vector3>();
            TargetBodyRotation = reader.GetUnmanaged<Quaternion>();
            Yaw = reader.GetFloat();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutUnmanaged(TargetPosition);
            writer.PutUnmanaged(TargetBodyRotation);
            writer.Put(Yaw);
        }
    }
}
