using UnityEngine.AI;
using UnityEngine;
using Gismo.Networking;
using Gismo.Networking.Core;

namespace Gismo.Quip.Cards
{
    public class MovementCard : CardHandler
    {
        public GameObject flag;
        Vector3 lastKnown;
        private float minMoveDistance = 0.1f;

        public override void Awake()
        {
            OnCardDroped += PrepairPacket;
            base.Awake();
        }

        public void PrepairPacket(Vector3 position)
        {
            flag.transform.position = position;
            if (Vector3.Distance(flag.transform.position, lastKnown) < minMoveDistance)
            {
                return;
            }

            lastKnown = flag.transform.position;

#if !UNITY_EDITOR
            switch (NetGameController.Instance.GetConnectionType())
            {
                case ConnectionType.Client:
                    Packet clientPacket = new Packet(NetworkPackets.ClientSentPackets.ClientPosition, NetGameController.Instance.GetUserID());

                    clientPacket.WriteVector2(flag.transform.position.ToVector2());

                    NetGameController.Instance.SendData_C(clientPacket);
                    break;
                case ConnectionType.Server:
                    Packet serverPacket = new Packet(NetworkPackets.ServerSentPackets.ClientPositionShare);

                    serverPacket.WriteByte(byte.MaxValue);
                    serverPacket.WriteVector2(flag.transform.position.ToVector2());

                    NetGameController.Instance.SendDataToAll_S(serverPacket);
                    break;
                default:
                    return;
            }
#endif
        }
    }
}
