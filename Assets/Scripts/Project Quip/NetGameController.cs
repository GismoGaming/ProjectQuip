using Gismo.Networking.Core;
using Gismo.Networking.Users;
using Gismo.Quip.Minerals;
using System.Collections;
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
    public struct PlayerDictionaryElement
    {
        public byte id;
        public SerializableVector2 position;
        public string username;
        public int role;
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

    public enum ConnectionType { NA, Server, Client };
    class NetGameController : Singleton<NetGameController>
    {
        private Client client;
        private Server server;

        private ConnectionType connectType;

        public bool DoDebug = false;

        [SerializeField] private GameObject[] playerPrefabs;

        private byte userID = byte.MaxValue;

        Dictionary<byte, PlayerController> clientPlayerControllerDictionary;

        Dictionary<uint, TrackedScript> trackedScriptDictionary;

        public delegate void Event();
        public Event onControllerIsReady;

        public Event onAssignedID;

        public Event onGameStart;

        [SerializeField] private Transform[] waitingRoomSpawnpoints;
        private int currentWaitingRoomID;

        [SerializeField] private Transform[] defaultSpawnpoints;
        private int defaultSpawnpointID;

        private bool newClient;

        [SerializeField] private List<string> defaultUsernames;

        const int RETRYCOUNT = 5;

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

        public void GetTrackedIDs()
        {
            foreach (KeyValuePair<uint, TrackedScript> t in trackedScriptDictionary)
            {
                Log($"{t.Key} | {t.Value.name}");
            }
        }

        public override void Awake()
        {
            base.Awake();

            clientPlayerControllerDictionary = new Dictionary<byte, PlayerController>();
            trackedScriptDictionary = new Dictionary<uint, TrackedScript>();
        }

        public Dictionary<byte, PlayerController> GetPlayers()
        {
            return clientPlayerControllerDictionary;
        }

        public PlayerController GetPlayer(byte id)
        {
            Log($"Searching for: {id}");
            return clientPlayerControllerDictionary[id];
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
            return (uint)trackedScriptDictionary.Count;
        }
        #endregion

        void RegisterClientPacketHandlers()
        {
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.FirstConnect, Client_FirstConnect_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare, Client_ClientPositionShare_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.ClientAttack, Client_ClientAttack_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.RoleChange, Client_RoleChange_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare, Client_PlayerDictionaryShare_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.PlayerHurt, Client_PlayerHurt_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.GroupTPRequest, Client_GroupTPRequest_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralSpawn, Client_MineralSpawn_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralDepositInit, Client_MineralDepositInit_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralNodeFound, Client_MineralNodeFound_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralStateChange, Client_MineralStateChange_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.MineralCollected, Client_MineralCollected_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.SpecialAbiltyPlace, Client_SpecialAbiltyPlace_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.CompressedSpawn, Client_CompressedSpawn_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.CompressedMineralStateChange, Client_CompressedMineralStateChange_Recieved);

            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.EnemyMove, Client_EnemyMove_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.EnemySpawn, Client_EnemySpawn_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.EnemyAttack, Client_EnemyAttack_Recieved);
            Networking.NetworkPackets.ClientFunctions.Add(Networking.NetworkPackets.ServerSentPackets.EnemyDamaged, Client_EnemyDamaged_Recieved);
        }

        void RegisterServerPacketHandlers()
        {
            Server.onClientConnected += (byte newID) =>
            {
                // Need to create the copy locally
                clientPlayerControllerDictionary.Add(newID, SpawnStartingGameObjects(newID, GetWaitingRoomSpawnpoint().position));

                Packet newClientInfo = new Packet(Networking.NetworkPackets.ServerSentPackets.FirstConnect);
                newClientInfo.WriteByte(newID);

                SendDataTo_S(newClientInfo, newID);

                // Send new player their ID alongside a player dictionary currently in use

                Log($"New player with ID of {newID} joined");

                // Update client's local copies
                SendDataToAll_S(GetClientArrayPacket());

                foreach (TrackedScript script in trackedScriptDictionary.Values)
                {
                    //Log($"Tracked Script OnNewClient Status: GO Name, {script.gameObject.name} and Status: {script != null}",Dev.DebugLogType.blue);

                    if (!script.OnNewClient().Equals(Networking.NetworkStatics.EMPTY))
                    {
                        SendDataTo_S(script.OnNewClient(), newID);
                    }
                    else
                    {
                        //Log($"{script.gameObject.name} has no OnNewClient, this isn't an issue, but it might be", Dev.DebugLogType.italics);
                    }
                }
            };

            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.ClientPosition, Server_ClientPositionPacketRecieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.RoleChange, Server_RoleChange_Recieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.ClientAttack, Server_ClientAttack_Recieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.PlayerInformationSend, Server_PlayerInformationSendRecieved);

            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.DepositLoseMineral, Server_ClientDepositLoseMineralPacketRecieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.MineralStateChange, Server_MineralStateChange_Recieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.MineralMineBegin, Server_ClientMineralMineBeginRecived);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.MineralCollected, Server_MineralCollected_Recieved);

            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.SpecialAbiltyPlace, Server_SpecialAbiltyPlace_Recieved);

            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.CompressedSpawn, Server_CompressedSpawn_Recieved);
            Networking.NetworkPackets.ServerFunctions.Add(Networking.NetworkPackets.ClientSentPackets.CompressedMineralStateChange, Server_CompressedMineralStateChange_Recieved);
        }

        #region Server Handles
        void Server_PlayerInformationSendRecieved(Packet packet, byte id)
        {
            clientPlayerControllerDictionary[id].SetUserName(packet.ReadString());
        }

        void Server_ClientPositionPacketRecieved(Packet packet, byte id)
        {
            Packet otherClientsPacket = new Packet(Networking.NetworkPackets.ServerSentPackets.ClientPositionShare);

            otherClientsPacket.WriteByte(id);
            packet.CopyTo(otherClientsPacket, true);

            UpdatePlayerControllerDictionary(id, packet.ReadVector2());

            SendDataToAllBut_S(otherClientsPacket, id);
        }

        void Server_ClientDepositLoseMineralPacketRecieved(Packet packet, byte id)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).OnDepositLoseMineral();
        }

        void Server_ClientMineralMineBeginRecived(Packet packet, byte id)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).StartDepositOutput();
        }

        void Server_MineralCollected_Recieved(Packet packet, byte id)
        {
            Packet clientPacket = new Packet(Networking.NetworkPackets.ServerSentPackets.MineralCollected);

            List<uint> ids = packet.ReadList<uint>();

            clientPacket.WriteList(ids);

            List<MineralCostDetails> costDetails = MineralCollectAction(ids);

            clientPacket.WriteList(costDetails);
            SendDataToAll_S(clientPacket);

            foreach (MineralCostDetails costDetail in costDetails)
            {
                CollectMineral(costDetail, id);
            }
        }

        void Server_RoleChange_Recieved(Packet packet, byte id)
        {
            int role = packet.ReadInt();

            SetUserRole(id, (Role)role);

            Packet others = new Packet(Networking.NetworkPackets.ServerSentPackets.RoleChange);
            others.WriteByte(id);
            others.WriteInt(role);
        }

        void Server_MineralStateChange_Recieved(Packet packet, byte id)
        {
            ((Mineral)GetTrackedScript(packet.ReadUint())).ChangeState(packet.ReadBoolean(), packet.ReadVector2(), false);
        }

        void Server_CompressedMineralStateChange_Recieved(Packet packet, byte id)
        {
            ((CompressedMineralCube)GetTrackedScript(packet.ReadUint())).ChangeState(packet.ReadBoolean(), packet.ReadVector2(), false);
        }

        void Server_SpecialAbiltyPlace_Recieved(Packet packet, byte id)
        {
            Packet toOthers = new Packet(Networking.NetworkPackets.ServerSentPackets.SpecialAbiltyPlace);
            packet.CopyTo(toOthers, true);

            PlaceDownNewRoleSpecificItem(packet.ReadUint(), packet.ReadVector2(), (Role)packet.ReadInt());

            SendDataToAllBut_S(toOthers, id);
        }

        void Server_ClientAttack_Recieved(Packet packet, byte id)
        {
            if (clientPlayerControllerDictionary.ContainsKey(id))
            {
                clientPlayerControllerDictionary[id].DoAttack();
            }

            Packet toOthers = new Packet(Networking.NetworkPackets.ServerSentPackets.ClientAttack);
            toOthers.WriteByte(id);

            SendDataToAllBut_S(toOthers, id);
        }

        void Server_CompressedSpawn_Recieved(Packet packet, byte id)
        {
            RoleSpecific.HaulerContainmentCube cubeSpawner = (RoleSpecific.HaulerContainmentCube)GetTrackedScript(packet.ReadUint());
            MineralCostDetails d = packet.ReadCostDetails();
            GameObject g = cubeSpawner.SpawnNewCube(d);

            Packet toClients = new Packet(Networking.NetworkPackets.ServerSentPackets.CompressedSpawn);
            toClients.WriteUint(cubeSpawner.ID);
            toClients.WriteUint(g.GetComponent<CompressedMineralCube>().ID);
            toClients.WriteCostDetails(d);
            toClients.WriteVector2(g.transform.position.ToVector2());

            SendDataToAll_S(toClients);
        }

        #endregion
        #region Client Handles

        void Client_PlayerHurt_Recieved(Packet packet)
        {
            GetPlayer(packet.ReadByte()).DoDamage();
        }

        void Client_FirstConnect_Recieved(Packet packet)
        {
            Log($"Got first connect packet");
            byte id = packet.ReadByte();

            client.clientID = id;

            Log($"I have gotten an id of: {id}");

            Client.onClientConnected?.Invoke(id);
        }

        void Client_EnemySpawn_Recieved(Packet packet)
        {
            Enemies.EnemySpawnManagment.Instance.SpawnEnemy(packet.ReadUint(), packet.ReadByte(), packet.ReadVector2());
        }

        void Client_EnemyAttack_Recieved(Packet packet)
        {
            ((Enemies.Enemy)GetTrackedScript(packet.ReadUint())).DoAttackAnimation();
        }

        void Client_EnemyDamaged_Recieved(Packet packet)
        {
            foreach (uint i in packet.ReadUintList())
            {
                ((Enemies.Enemy)GetTrackedScript(i)).DoDamage();
            }
        }

        void Client_MineralStateChange_Recieved(Packet packet)
        {
            ((Mineral)GetTrackedScript(packet.ReadUint())).ChangeState(packet.ReadBoolean(), packet.ReadVector2(), false);
        }

        void Client_CompressedMineralStateChange_Recieved(Packet packet)
        {
            ((CompressedMineralCube)GetTrackedScript(packet.ReadUint())).ChangeState(packet.ReadBoolean(), packet.ReadVector2(), false);
        }

        void Client_SpecialAbiltyPlace_Recieved(Packet packet)
        {
            PlaceDownNewRoleSpecificItem(packet.ReadUint(), packet.ReadVector2(), (Role)packet.ReadInt());
        }

        void Client_ClientAttack_Recieved(Packet packet)
        {
            byte pid = packet.ReadByte();

            if (clientPlayerControllerDictionary.ContainsKey(pid))
            {
                clientPlayerControllerDictionary[pid].DoAttack();
            }
        }

        void Client_MineralDepositInit_Recieved(Packet packet)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).Initalize(packet);
        }

        void Client_ClientPositionShare_Recieved(Packet packet)
        {
            UpdatePlayerControllerDictionary(packet.ReadByte(), packet.ReadVector2());
        }

        void Client_MineralNodeFound_Recieved(Packet packet)
        {
            ((MineralDeposit)GetTrackedScript(packet.ReadUint())).HasBeenFound(packet.ReadByte());
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
            foreach (Tracked2DPositionByted p in packet.ReadListTracked2DPositionByted())
            {
                if (clientPlayerControllerDictionary.ContainsKey(p.id))
                {
                    clientPlayerControllerDictionary[p.id].SetPosition(p.position);
                }
            }
        }

        void Client_PlayerDictionaryShare_Recieved(Packet packet)
        {
            foreach (PlayerDictionaryElement p in packet.ReadListPlayerDictionaryElement())
            {
                UpdatePlayerControllerDictionary(p.id, p.position);

                clientPlayerControllerDictionary[p.id].SetUserName(p.username);
                clientPlayerControllerDictionary[p.id].UpdateRole((Role)p.role);
            }

            if (newClient)
            {
                onAssignedID?.Invoke();
                newClient = false;
            }
        }

        void Client_MineralCollected_Recieved(Packet packet)
        {
            byte playerID = packet.ReadByte();
            foreach (uint i in packet.ReadList<uint>())
            {
                Destroy(GetTrackedScript(i).gameObject);
            }

            foreach (MineralCostDetails costDetail in packet.ReadList<MineralCostDetails>())
            {
                CollectMineral(costDetail, playerID);
            }
        }

        void Client_RoleChange_Recieved(Packet packet)
        {
            byte playerID = packet.ReadByte();

            int role = packet.ReadInt();

            SetUserRole(playerID, (Role)role);
        }

        void Client_CompressedSpawn_Recieved(Packet packet)
        {
            uint spawnerID = packet.ReadUint();
            uint itemID = packet.ReadUint();

            Log($"{spawnerID}");

            MineralCostDetails details = packet.ReadCostDetails();
            Log($"{details.price}");

            Vector2 pos = packet.ReadVector2();
            Log($"{pos}");

            ((RoleSpecific.HaulerContainmentCube)GetTrackedScript(spawnerID)).SpawnNewCubeWithID(details, pos, itemID);
        }

        void Client_EnemyMove_Recieved(Packet packet)
        {
            ((Enemies.Enemy)GetTrackedScript(packet.ReadUint())).GoToPoint(packet.ReadVector2());
        }

        #endregion

        #region Generic Functions

        void ClearDictionaries()
        {
            clientPlayerControllerDictionary = new Dictionary<byte, PlayerController>();
            trackedScriptDictionary = new Dictionary<uint, TrackedScript>();
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
        void UpdatePlayerControllerDictionary(byte id, Vector2 pos)
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
            Log($"{id} -> {s.name}");
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

        void PlaceDownNewRoleSpecificItem(uint ID, Vector2 position, Role role)
        {
            GameObject newObject = Instantiate(PlayerCentralization.Instance.roleSpecialsTable[role].prefab, position, Quaternion.identity);
            newObject.GetComponent<TrackedGameObject>().GlobalPlace(ID, position);
        }

        public void CollectMineral(MineralCostDetails costDetails, byte collectingPlayerID)
        {
#if UNITY_EDITOR
            string collectingUsername = GetLocalPlayer().GetUserName();
# else
            string collectingUsername = clientPlayerControllerDictionary[collectingPlayerID].GetUserName();
#endif
            Notification.PushNotification($"{collectingUsername} has collected a {costDetails.name} for {costDetails.price} coins");
        }

        public void SetUserRole(byte userID, Role newRole)
        {
#if UNITY_EDITOR
            GetLocalPlayer().UpdateRole(newRole);
#else
            clientPlayerControllerDictionary[userID].UpdateRole(newRole);
#endif
        }

        public void SetUserRole(Role newRole)
        {
            PlayerCentralization.Instance.playerRole = newRole;

            GetLocalPlayer().UpdateRole(newRole);

            if (IsConnectedAs(ConnectionType.Server))
            {
                Packet p = new Packet(Networking.NetworkPackets.ServerSentPackets.RoleChange);
                p.WriteByte(Networking.NetworkStatics.ServerID);
                p.WriteInt((int)newRole);

                SendDataToAll_S(p);
            }
            else
            {
                Packet p = new Packet(Networking.NetworkPackets.ClientSentPackets.RoleChange, GetUserID());
                p.WriteInt((int)newRole);

                SendData_C(p);
            }
        }

        public Packet GetClientArrayPacket()
        {
            Packet p = new Packet(Networking.NetworkPackets.ServerSentPackets.PlayerDictionaryShare);

            List<PlayerDictionaryElement> playerPositions = new List<PlayerDictionaryElement>();
            foreach (KeyValuePair<byte, PlayerController> i in clientPlayerControllerDictionary)
            {
                PlayerDictionaryElement newPos = new PlayerDictionaryElement
                {
                    id = i.Key,
                    position = new SerializableVector2(i.Value.transform.position.ToVector2()),
                    username = i.Value.GetUserName(),
                    role = (int)i.Value.GetRole()
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

            onGameStart?.Invoke();
        }

        #endregion

        #region Client
        public void StartClient(string ip = "localhost")
        {
            DoClientConnect(ip);

            StartCoroutine(ConnectCheck(ip));
        }

        void DoClientConnect(string ip)
        {
            ClearDictionaries();
            Log($"Connecting to {ip}");
            connectType = ConnectionType.Client;
            Client.onClientConnected = null;
            client = new Client();

            RegisterClientPacketHandlers();

            Client.onClientConnected += (byte i) =>
            {
                Log($"Gott id {i}");
                userID = i;
                newClient = true;
            };

            client.Connect(ip);

            onControllerIsReady?.Invoke();
        }

        IEnumerator ConnectCheck(string ip)
        {
            int retries = 0;
            yield return new WaitForSeconds(1f);
            while (retries < RETRYCOUNT)
            {
                if (IsConnected() && userID != byte.MaxValue)
                    break;

                DoClientConnect(ip);
                yield return new WaitForSecondsRealtime(1f);
                Disconnect();
                retries++;
            }
            Log($"Connected status -> {IsConnected()}");
        }

        public void SendData_C(Packet packet)
        {
            if (IsConnectedAs(ConnectionType.Client))
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
            ClearDictionaries();
            connectType = ConnectionType.Server;
            server = new Server(3);
            Server.onClientConnected = null;
            Server.onServerUp = null;

            RegisterServerPacketHandlers();
            Server.onServerUp += (byte newID) =>
            {
                userID = newID;
                // Need to create the copy locally
                clientPlayerControllerDictionary.Add(newID, SpawnStartingGameObjects(newID, GetWaitingRoomSpawnpoint().position));
            };
            Server.onClientDisconnect += (byte id) =>
            {
                if(clientPlayerControllerDictionary.ContainsKey(id))
                {
                    clientPlayerControllerDictionary[id].gameObject.transform.position = Vector3.one * 500f;
                    clientPlayerControllerDictionary[id].gameObject.SetActive(false);

                    clientPlayerControllerDictionary.Remove(id);
                }
            };
            server.StartListening();
            onControllerIsReady?.Invoke();
            onAssignedID?.Invoke();
            Log("Connected as Server");
        }

        public void SendDataToAll_S(Packet packet)
        {
            if (IsConnectedAs(ConnectionType.Server))
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