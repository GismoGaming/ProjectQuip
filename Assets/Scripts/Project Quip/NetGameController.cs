using Gismo.Networking.Core;
using Gismo.Networking.Users;
using System.Collections.Generic;
using UnityEngine;
using static DL;

namespace Gismo.Quip
{
    public enum ConnectionType { NA, Server, Client};
    class NetGameController : MonoBehaviour
    {
        public static NetGameController instance;

        private Client client;
        private Server server;

        private ConnectionType connectType;

        public bool DoDebug = false;

        [SerializeField] private GameObject playerPrefab;

        private int userID;

        Dictionary<int, PlayerMovement> clientIDDictionary;

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

                clientIDDictionary = new Dictionary<int, PlayerMovement>();
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
            Log("Got player dict packet recieve");
            int x = packet.ReadInt();
            Log($"{x}");
            for (int i = 0; i <= x; i++)
            {
                int playerID = packet.ReadInt();
                Vector3 position = packet.ReadVector3();
                Log($"ID: {playerID} \t startPos: {position}");

                HandleClientPositionPacket(playerID, position);
            }
        }

        void HandleClientPositionPacket(int id, Vector3 pos)
        {
            if(DoDebug)
                Log($"Got packet for id of {id} and data of {pos}");

            if (clientIDDictionary.ContainsKey(id))
            {
                clientIDDictionary[id].UpdatePosition(pos);
            }
            else
            {
                clientIDDictionary.Add(id, SpawnStartingGameObjects(id, pos));
            }
        }

        PlayerMovement SpawnStartingGameObjects(int id, Vector3 startPos)
        {
            PlayerMovement p = Instantiate(playerPrefab).GetComponent<PlayerMovement>();
            p.transform.position = startPos;

            p.Initalize(id);
            return p;
        }

        public Packet GetClientArrayPacket()
        {
            Packet p = new Packet(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare);

            p.WriteInt(clientIDDictionary.Count);

            Log($"{clientIDDictionary.Count}");

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
            server = new Server(10);
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