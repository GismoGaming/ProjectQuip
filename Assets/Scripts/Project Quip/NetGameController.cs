using Gismo.Networking.Core;
using Gismo.Networking.Users;
using System.Collections.Generic;
using UnityEngine;
using static DL;

namespace Gismo.Quip
{
    [System.Serializable]
    public struct LeanTweenMovement
    {
        public float timing;
        public LeanTweenType type;
    }
    public enum ConnectionType { NA, Server, Client};
    class NetGameController : MonoBehaviour
    {
        public static NetGameController instance;

        private Client client;
        private Server server;

        private ConnectionType connectType;

        public bool DoDebug = false;

        [SerializeField] private GameObject[] playerPrefabs;

        private int userID;

        Dictionary<int, PlayerController> clientIDDictionary;

        public string f_gnetHelper()
        {
            string c = "{";
            foreach (int k in clientIDDictionary.Keys)
            {
                c += $"{k},";
            }
            c += "}";
            return c;
        }

        private void Awake()
        {
            if (!instance)
            {
                connectType = ConnectionType.NA;

                instance = this;

                clientIDDictionary = new Dictionary<int, PlayerController>();
            }
            else
            {
                Destroy(this);
            }
        }

        #region Helper Functions

        public bool IsConnected()
        {
            switch (connectType)
            {
                case ConnectionType.Server:
                    return server.IsConnected();
                case ConnectionType.Client:
                    return client.IsConnected;
                default:
                case ConnectionType.NA:
                    return false;
            }
        }

        public int GetUserID()
        {
            return userID;
        }

        public ConnectionType GetConnectionType()
        {
            return connectType;
        }

        public bool IsConnectedAs(ConnectionType type)
        {
            if (connectType == ConnectionType.NA)
                return false;
            return connectType == type && IsConnected();
        }

        public void Disconnect()
        {
            switch (connectType)
            {
                case ConnectionType.Client:
                    client.Disconnect();
                    break;
                case ConnectionType.Server:
                    server.StopListening();
                    break;
            }
        }
            #endregion

        void RegisterClientPacketHandlers()
        {
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare,Client_ClientPositionShare_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare,Client_PlayerDictionaryShare_Recieved);
        }

        void RegisterServerPacketHandlers()
        {
            Server.onClientConnected += (int newID) => 
            {
                GismoThreading.ExecuteInNormalUpdate(() =>
                {
                    // Need to create the copy locally
                    clientIDDictionary.Add(newID, SpawnStartingGameObjects(newID, Vector3.zero));

                    // Update client's local copies
                    SendDataToAll_S(GetClientArrayPacket());
                });
            };
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.ClientPosition, Server_ClientPositionPacketRecieved);
        }

        void Server_ClientPositionPacketRecieved(Packet packet, int id)
        {
            Packet otherClientsPacket = new Packet(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare);

            otherClientsPacket.WriteInt(id);
            otherClientsPacket.WriteBlock(packet.GetData(true));

            HandleClientPositionPacket(id, packet.ReadVector3());

            SendDataToAllBut_S(otherClientsPacket, id);
        }

        void Client_ClientPositionShare_Recieved(Packet packet)
        {
            HandleClientPositionPacket(packet.ReadInt(), packet.ReadVector3());
        }

        void Client_PlayerDictionaryShare_Recieved(Packet packet)
        {
            int clientCount = packet.ReadInt();
            for (int i = 1; i <= clientCount; i++)
            {
                HandleClientPositionPacket(packet.ReadInt(), packet.ReadVector3());
            }
        }

        void HandleClientPositionPacket(int id, Vector3 pos)
        {
            if (clientIDDictionary.ContainsKey(id))
            {
                clientIDDictionary[id].UpdatePosition(pos);
            }
            else
            {
                clientIDDictionary.Add(id, SpawnStartingGameObjects(id, pos));
            }
        }

        PlayerController SpawnStartingGameObjects(int id, Vector3 startPos)
        {
            PlayerController controller = null;
            Generic.SimpleFollow followController = null;

            foreach (GameObject g in playerPrefabs)
            {
                GameObject newObject = Instantiate(g);

                newObject.transform.position = startPos;
                if (newObject.TryGetComponent(out PlayerController p))
                {
                    p.Initalize(id, startPos);

                    controller = p;
                }

                if (newObject.TryGetComponent(out Generic.SimpleFollow s))
                {
                    followController = s;
                }
            }

            followController.SetTarget(controller.gameObject.transform);

            return controller;
        }

        public Packet GetClientArrayPacket()
        {
            Packet p = new Packet(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare);

            p.WriteInt(clientIDDictionary.Count);

            foreach(int i in clientIDDictionary.Keys)
            {
                p.WriteInt(i);
                p.WriteVector3(clientIDDictionary[i].transform.position);
            }

            return p;
        }

        #region Client
        public void StartClient(string ip = "localHost")
        {
            connectType = ConnectionType.Client;
            Client.onClientConnected = null;
            client = new Client();

            RegisterClientPacketHandlers();
            Client.onClientConnected += (int i) => { userID = i; };

            client.Connect(ip);

            Log("Connected as Client");
        }

        public void SendData_C(Packet packet)
        {
            if(IsConnectedAs(ConnectionType.Client))
            {
                client.SendData(packet);
            }
        }
        #endregion

        #region Server
        public void StartServer()
        {
            connectType = ConnectionType.Server;
            server = new Server(3);
            Server.onClientConnected = null;
            Server.onServerUp = null;

            RegisterServerPacketHandlers();
            Server.onServerUp += (int newID) =>
            {
                userID = newID;
                // Need to create the copy locally
                clientIDDictionary.Add(newID, SpawnStartingGameObjects(newID,Vector3.zero));
            };

            server.StartListening();

            Log("Connected as Server");
        }

        public void SendDataToAll_S(Packet packet)
        {
            if(IsConnectedAs(ConnectionType.Server))
            {
                server.SendDataToAll(packet);
            }
        }

        public void SendDataTo_S(Packet packet, int playerID)
        {
            if (IsConnectedAs(ConnectionType.Server) && server.IsPlayerIndexConnected(playerID))
            {
                server.SendDataTo(playerID, packet);
            }
        }

        public void SendDataToAllBut_S(Packet packet, int playerID)
        {
            if (IsConnectedAs(ConnectionType.Server) && server.IsPlayerIndexConnected(playerID))
            {
                server.SendDataToAllBut(playerID, packet);
            }
        }
        #endregion
    }
}