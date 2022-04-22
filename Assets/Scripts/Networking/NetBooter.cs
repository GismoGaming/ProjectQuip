using Gismo.Networking.Core;
using Gismo.Quip;
using UnityEngine;

namespace Gismo.Networking
{
    public class NetBooter : Singleton<NetBooter>
    {
        public static bool connectAsServer;
        public static string ip;

        string userName;

        public void StartServer()
        {
            userName = NetGameController.Instance.GetRandomUserName();
            NetGameController.Instance.onControllerIsReady += () =>
            {
                NetGameController.Instance.GetLocalPlayer().SetUserName(userName);
            };

            NetGameController.Instance.StartServer();
        }

        public void StartClient()
        {
            userName = NetGameController.Instance.GetRandomUserName();
            NetGameController.Instance.onAssignedID += () =>
            {
                NetGameController.Instance.GetLocalPlayer().SetUserName(userName);
                Packet usernamePacket = new Packet(NetworkPackets.ClientSentPackets.PlayerInformationSend, NetGameController.Instance.GetUserID());

                usernamePacket.WriteString(userName);

                NetGameController.Instance.SendData_C(usernamePacket);
            };

            NetGameController.Instance.StartClient(ip);
        }
    }
}
