using Gismo.Networking;
using Gismo.Networking.Core;
using UnityEngine;

namespace Gismo.Quip.Cards
{
    public class MovementCard : CardDropBase
    {
        public GameObject flag;
        Vector3 lastKnown;
        private float minMoveDistance = 0.1f;

        public override void Awake()
        {
            OnCardDown += PrepairPacket;
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
            switch (NetGameController.instance.GetConnectionType())
            {
                case ConnectionType.Client:
                    Packet clientPacket = new Packet(NetworkPackets.ClientSentPackets.ClientPosition, NetGameController.instance.GetUserID());

                    clientPacket.WriteVector3(flag.transform.position);

                    NetGameController.instance.SendData_C(clientPacket);
                    break;
                case ConnectionType.Server:
                    Packet serverPacket = new Packet(NetworkPackets.ServerSentPackets.ClientPositionShare);

                    serverPacket.WriteInt(-1);
                    serverPacket.WriteVector3(flag.transform.position);

                    NetGameController.instance.SendDataToAll_S(serverPacket);
                    break;
                default:
                    return;
            }
#endif
        }
    }
}
