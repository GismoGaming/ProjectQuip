using System.Collections;
using Gismo.Networking.Core;
using UnityEngine;

namespace Gismo.Quip
{
    public class TrackedGameObject : TrackedScript
    {
        public virtual void OnPlacedDownPacket(){   }

        public virtual void OnLocalPlace() { }
        public virtual void OnGlobalPlace() { }

        public void LocalPlace()
        {
            GenerateID();

            OnPlacedDownPacket();

            OnLocalPlace();

            base.OnRegisterReady();
        }

        public void GlobalPlace(uint id, Vector2 position)
        {
            transform.position = position.ToVector3();

            ID = id;

            OnGlobalPlace();

            base.OnRegisterReady();
        }

        public Packet GetPositionPacketServer(Networking.NetworkPackets.ServerSentPackets s)
        {
            Packet packet = GetPacketServer(s);
            packet.WriteVector2(transform.position.ToVector2());
            return packet;
        }

        public Packet GetPositionPacketClient(Networking.NetworkPackets.ClientSentPackets s)
        {
            Packet packet = GetPacketClient(s, NetGameController.Instance.GetUserID());
            packet.WriteVector2(transform.position.ToVector2());
            return packet;
        }
    }
}
