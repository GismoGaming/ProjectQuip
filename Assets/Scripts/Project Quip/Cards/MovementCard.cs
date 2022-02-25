using UnityEngine.AI;
using UnityEngine;
using Gismo.Networking;
using Gismo.Networking.Core;

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
            switch (NetGameController.Instance.GetConnectionType())
            {
                case ConnectionType.Client:
                    Packet clientPacket = new Packet(NetworkPackets.ClientSentPackets.ClientPosition, NetGameController.Instance.GetUserID());

                    clientPacket.WriteVector3(flag.transform.position);

                    NetGameController.Instance.SendData_C(clientPacket);
                    break;
                case ConnectionType.Server:
                    Packet serverPacket = new Packet(NetworkPackets.ServerSentPackets.ClientPositionShare);

                    serverPacket.WriteInt(-1);
                    serverPacket.WriteVector3(flag.transform.position);

                    NetGameController.Instance.SendDataToAll_S(serverPacket);
                    break;
                default:
                    return;
            }
#endif
        }
    }
}
