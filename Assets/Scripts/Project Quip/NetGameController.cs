using Gismo.Networking.Core;
using Gismo.Networking.Users;
using Gismo.Quip.Minerals;
using System.Collections.Generic;
using UnityEngine;
using static DL;

namespace Gismo.Quip
{
    [System.Serializable]
    public struct Tracked2DPositionByted
    {
        public byte id;
        public SerializableVector2 position;
    }

    [System.Serializable]
    public struct Tracked2DPositionUint
    {
        public uint id;
        public SerializableVector2 position;
    }


    [System.Serializable]
    public struct LeanTweenMovement
    {
        public float timing;
        public LeanTweenType type;
    }
    public enum ConnectionType { NA, Server, Client};
    class NetGameController : Singleton<NetGameController>
    {
        private Client client;
        private Server server;

        private ConnectionType connectType;

        public bool DoDebug = false;

        [SerializeField] private GameObject[] playerPrefabs;

        private byte userID = byte.MinValue;

        Dictionary<byte, PlayerController> clientPlayerControllerDictionary;

        Dictionary<uint, TrackedScript> trackedScriptDictionary;

        public delegate void OnControllerReady();
        public OnControllerReady onControllerIsReady;

        public delegate void OnAssignedID();
        public OnAssignedID onAssignedID;

        [SerializeField] private Transform[] waitingRoomSpawnpoints;
        private int currentWaitingRoomID;

        [SerializeField] private Transform[] defaultSpawnpoints;
        private int defaultSpawnpointID;

        private bool newClient;

        [SerializeField] private List<string> defaultUsernames;

        public Transform GetWaitingRoomSpawnpoint()
        {
            currentWaitingRoomID++;
            currentWaitingRoomID = Mathf.Clamp(currentWaitingRoomID, 0, waitingRoomSpawnpoints.Length - 1);

            return waitingRoomSpawnpoints[currentWaitingRoomID];
        }

        private Transform GetDefaultSpawnpoint()
        {
            defaultSpawnpointID++;
            defaultSpawnpointID = Mathf.Clamp(defaultSpawnpointID, 0, defaultSpawnpoints.Length - 1);

            return defaultSpawnpoints[defaultSpawnpointID];
        }

        public string f_gnetHelper()
        {
            string c = "{";
            foreach (int k in clientPlayerControllerDictionary.Keys)
            {
                c += $"{k},";
            }
            c += "}";
            return c;
        }

        public override void Awake()
        {
            base.Awake();

            clientPlayerControllerDictionary = new Dictionary<byte, PlayerController>();
            trackedScriptDictionary = new Dictionary<uint, TrackedScript>();
        }

        #region Helper Functions

        public bool IsConnected()
        {
#if UNITY_EDITOR
            return true;
#else
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
#endif
        }

        public byte GetUserID()
        {
            return userID;
        }

        public ConnectionType GetConnectionType()
        {
            return connectType;
        }

        public bool IsConnectedAs(ConnectionType type)
        {
#if UNITY_EDITOR
            return type == ConnectionType.NA || type == ConnectionType.Server;
#else
            if (connectType == ConnectionType.NA)
                return false;
            return connectType == type && IsConnected();
#endif
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

        private bool ScriptIsTracked(Packet packet, out uint ID)
        {
            ID = packet.ReadUint();

            return trackedScriptDictionary.ContainsKey(ID);
        }
        
        public TrackedScript GetTrackedScript(uint ID)
        {
            if (trackedScriptDictionary.ContainsKey(ID))
                return trackedScriptDictionary[ID];
            return null;
        }

        public string GetRandomUserName()
        {
            return defaultUsernames.GetRandomItem();
        }

        public PlayerController GetLocalPlayer()
        {
#if UNITY_EDITOR
            return FindObjectOfType<PlayerController>();
#else
            if(IsConnected())
            {
                return clientPlayerControllerDictionary[GetUserID()];
            }

            Log("You are not connected!");
            return null;
#endif
        }

        public uint GetNextTrackedID()
        {
            return ((uint)trackedScriptDictionary.Count) + 1;
        }
#endregion

        void RegisterClientPacketHandlers()
        {
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare,Client_ClientPositionShare_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare,Client_PlayerDictionaryShare_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralSpawn,Client_MineralSpawn_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralDepositInit, Client_MineralDepositInit_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.PickupMineral, Client_MineralPickupPacketRecieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.GroupTPRequest, Client_GroupTPRequest_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralCollected,Client_MineralCollected_Recieved);
        }

        void RegisterServerPacketHandlers()
        {
            Server.onClientConnected += (byte newID) => 
            {
                GismoThreading.ExecuteInNormalUpdate(() =>
                {
                    // Need to create the copy locally
                    clientPlayerControllerDictionary.Add(newID, SpawnStartingGameObjects(newID, GetWaitingRoomSpawnpoint().position));

                    // Update client's local copies
                    SendDataToAll_S(GetClientArrayPacket());

                    foreach (TrackedScript script in trackedScriptDictionary.Values)
                    {
                        Log($"Tracked Script OnNewClient Status: GO Name, {script.gameObject.name} and Status: {script != null}",Dev.DebugLogType.blue);

                        if (!script.OnNewClient().Equals(Networking.NetworkStatics.EMPTY))
                        {
                            SendDataTo_S(script.OnNewClient(), newID);
                        }
                        else
                        {
                            Log($"{script.gameObject.name} has no OnNewClient, this isn't an issue, but it might be", Dev.DebugLogType.italics);
                        }
                    }
                });
            };

            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.ClientPosition, Server_ClientPositionPacketRecieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.DepositLoseMineral, Server_ClientDepositLoseMineralPacketRecieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.PickupMineral, Server_ClientPickupMineralPacketRecieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.MineralMineBegin, Server_ClientMineralMineBeginRecived);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.MineralCollected, Server_MineralCollected_Recieved);
        }

        void Server_ClientPositionPacketRecieved(Packet packet, byte id)
        {
            Packet otherClientsPacket = new Packet(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare);

            otherClientsPacket.WriteByte(id);
            packet.CopyTo(otherClientsPacket, true);

            HandleClientPositionPacket(id, packet.ReadVector2());

            SendDataToAllBut_S(otherClientsPacket, id);
        }

        void Server_ClientDepositLoseMineralPacketRecieved(Packet packet, byte id)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).OnDepositLoseMineral();
        }

        void Server_ClientPickupMineralPacketRecieved(Packet packet, byte id)
        {
            uint TSID = packet.ReadUint();
            Mineral mineral = (Mineral)GetTrackedScript(TSID);
            mineral.OnPickup();

            SendDataToAll_S(mineral.GetPacketServer(Networking.NetworkPackets.ServerSentPackets.PickupMineral));
        }

        void Server_ClientMineralMineBeginRecived(Packet packet, byte id)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).StartDepositOutput();
        }
        void Server_MineralCollected_Recieved(Packet packet,byte id)
        {
            Packet clientPacket = new Packet(Networking.NetworkPackets.ServerSentPackets.MineralCollected);

            List<uint> ids = packet.ReadList<uint>();

            clientPacket.WriteList(ids);

            List<MineralCostDetails> costDetails = MineralCollectAction(ids);

            clientPacket.WriteList(costDetails);
            SendDataToAll_S(clientPacket);

            foreach (MineralCostDetails costDetail in costDetails)
            {
                CollectMineral(costDetail,id);
            }
        }

        public List<MineralCostDetails> MineralCollectAction(List<uint> mineralIDS)
        {
            List<MineralCostDetails> costDetails = new List<MineralCostDetails>();
            foreach (uint i in mineralIDS)
            {
                Mineral mineral = GetTrackedScript(i) as Mineral;

                costDetails.Add(MineralDatabase.Instance.GetCostDetails(mineral.GetMineralType()));

                Destroy(mineral.gameObject);
            }
            return costDetails;
        }

        void Client_MineralDepositInit_Recieved(Packet packet)
        {
            if (ScriptIsTracked(packet, out uint ID))
            {
                ((MineralDeposit)trackedScriptDictionary[ID]).Initalize(packet);
            }
            else
            {
                Log($"We don't have an entry for id {ID}",Dev.DebugLogType.error);
            }
        }

        void Client_MineralPickupPacketRecieved(Packet packet)
        {
            ((Mineral)GetTrackedScript(packet.ReadUint())).OnPickup();
        }

        void Client_ClientPositionShare_Recieved(Packet packet)
        {
            HandleClientPositionPacket(packet.ReadByte(), packet.ReadVector2());
        }

        void Client_MineralSpawn_Recieved(Packet packet)
        {
            uint depotID = packet.ReadUint();
            int amount = packet.ReadInt();

            List<Tracked2DPositionUint> positions = new List<Tracked2DPositionUint>();

            for (int i = 0; i < amount; i++)
            {
                uint mineralID = packet.ReadUint();
                Vector2 mineralPosition = packet.ReadVector2();

                positions.Add(new Tracked2DPositionUint()
                {
                    id = mineralID,
                    position = new SerializableVector2(mineralPosition)
                });
            }
            ((MineralDeposit)trackedScriptDictionary[depotID]).SpawnMineral(positions);
        }

        void Client_GroupTPRequest_Recieved(Packet packet)
        {
            foreach (Tracked2DPositionByted p in packet.ReadList<Tracked2DPositionByted>())
            {
                if (clientPlayerControllerDictionary.ContainsKey(p.id))
                {
                    clientPlayerControllerDictionary[p.id].SetPosition(p.position);
                }
            }
        }

        void Client_PlayerDictionaryShare_Recieved(Packet packet)
        {
            foreach (Tracked2DPositionByted p in packet.ReadList<Tracked2DPositionByted>())
            {
                HandleClientPositionPacket(p.id, p.position);
            }

            if(newClient)
            {
                onAssignedID?.Invoke();
                newClient = false;
            }
        }

        void Client_MineralCollected_Recieved(Packet packet)
        {
            byte playerID = packet.ReadByte();
            foreach(uint i in packet.ReadList<uint>())
            {
                Destroy(GetTrackedScript(i).gameObject);
            }

            foreach (MineralCostDetails costDetail in packet.ReadList<MineralCostDetails>())
            {
                CollectMineral(costDetail, playerID);
            }
        }

        void HandleClientPositionPacket(byte id, Vector2 pos)
        {
            if (clientPlayerControllerDictionary.ContainsKey(id))
            {
                clientPlayerControllerDictionary[id].UpdatePosition(pos);
            }
            else
            {
                clientPlayerControllerDictionary.Add(id, SpawnStartingGameObjects(id, pos));
            }
        }

        public void RegisterTrackedScript(uint id, TrackedScript s)
        {
            trackedScriptDictionary.Add(id, s);
        }

        PlayerController SpawnStartingGameObjects(byte id, Vector3 startPos)
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

        public void CollectMineral(MineralCostDetails costDetails, byte collectingPlayerID)
        {
            Log($"A {costDetails.name} has been collected with price of {costDetails.price}");
        }

        public Packet GetClientArrayPacket()
        {
            Packet p = new Packet(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare);

            List<Tracked2DPositionByted> playerPositions = new List<Tracked2DPositionByted>();
            foreach (byte i in clientPlayerControllerDictionary.Keys)
            {
                Tracked2DPositionByted newPos = new Tracked2DPositionByted
                {
                    id = i,
                    position = new SerializableVector2(clientPlayerControllerDictionary[i].transform.position.ToVector2())
                };

                playerPositions.Add(newPos);
            }

            p.WriteList(playerPositions);

            return p;
        }

        public void BeginPlaySession()
        {
            if (IsConnectedAs(ConnectionType.Server))
            {
#if UNITY_EDITOR
                GetLocalPlayer().SetPosition(GetDefaultSpawnpoint().position);
#else
                List<Tracked2DPositionByted> positions = new List<Tracked2DPositionByted>();
                Packet packet = new Packet(Networking.NetworkPackets.ServerSentPackets.GroupTPRequest);
                foreach (KeyValuePair<byte,PlayerController> player in clientPlayerControllerDictionary)
                {
                    Vector3 pos = GetDefaultSpawnpoint().position;
                    player.Value.SetPosition(pos);

                    positions.Add(new Tracked2DPositionByted
                    {
                        id = player.Key,
                        position = new SerializableVector2(pos.ToVector2())
                    });
                }

                packet.WriteList(positions);
                SendDataToAll_S(packet);
#endif
            }
            else
            {
                Log("You are not the server owner, tell your server owner to type this to start the game");
            }
        }

#region Client
        public void StartClient(string ip = "localHost")
        {
            connectType = ConnectionType.Client;
            Client.onClientConnected = null;
            client = new Client();

            RegisterClientPacketHandlers();

            Client.onClientConnected += (byte i) => { 
                userID = i;
                newClient = true;
            };

            client.Connect(ip);

            onControllerIsReady?.Invoke();
            Log("Connected as Client");
        }

        public void SendData_C(Packet packet)
        {
            if(IsConnectedAs(ConnectionType.Client))
            {
#if !UNITY_EDITOR
                client.SendData(packet);
#endif
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
            Server.onServerUp += (byte newID) =>
            {
                userID = newID;
                // Need to create the copy locally
                clientPlayerControllerDictionary.Add(newID, SpawnStartingGameObjects(newID,GetWaitingRoomSpawnpoint().position));
            };

            server.StartListening();
            onControllerIsReady?.Invoke();
            onAssignedID?.Invoke();
            Log("Connected as Server");
        }

        public void SendDataToAll_S(Packet packet)
        {
            if(IsConnectedAs(ConnectionType.Server))
            {
#if !UNITY_EDITOR
                server.SendDataToAll(packet);
#endif
            }
        }

        public void SendDataTo_S(Packet packet, byte playerID)
        {
            if (IsConnectedAs(ConnectionType.Server) && server.IsPlayerIndexConnected(playerID))
            {
#if !UNITY_EDITOR
                server.SendDataTo(playerID, packet);
#endif
            }
        }

        public void SendDataToAllBut_S(Packet packet, byte playerID)
        {
            if (IsConnectedAs(ConnectionType.Server) && server.IsPlayerIndexConnected(playerID))
            {
#if !UNITY_EDITOR
                server.SendDataToAllBut(playerID, packet);
#endif
            }
        }
#endregion
    }
}