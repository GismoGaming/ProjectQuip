using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using static DL;

namespace Gismo.Networking.Users
{
    public delegate void ClientFunction(byte id);

    public sealed class Server : IDisposable
    {
        private Dictionary<byte, Socket> socketLookupTable;
        private List<byte> playerIDs;
        private Socket listenerSocket;

        public int clientLimit { get; }

        public bool serverListening { get; private set; }

        public byte currentMaxPlayerID { get; private set; }

        public static ClientFunction onServerUp;
        public static ClientFunction onClientConnected;

        public Server(int clientLimit = 0)
        {
            NetworkPackets.InitalizeFunctions();
            if (listenerSocket != null || socketLookupTable != null)
            {
                return;
            }
            socketLookupTable = new Dictionary<byte, Socket>();
            this.clientLimit = clientLimit;
        }

        public void StartListening()
        {
            if (socketLookupTable == null || serverListening || listenerSocket != null)
            {
                return;
            }
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenerSocket.Bind(new IPEndPoint(IPAddress.Any, NetworkStatics.portNumberTCP));
            serverListening = true;
            listenerSocket.Listen(NetworkStatics.maxServerConnections);
            listenerSocket.BeginAccept(new AsyncCallback(DoAcceptClient), byte.MinValue);

            onServerUp?.Invoke(NetworkStatics.ServerID);

            Log($"Started Server on port {NetworkStatics.portNumberTCP}, with max connections {NetworkStatics.maxServerConnections}");
        }

        public void StopListening()
        {
            if (!serverListening || socketLookupTable == null)
            {
                return;
            }
            serverListening = false;
            if (listenerSocket == null)
            {
                return;
            }
            listenerSocket.Close();
            listenerSocket.Dispose();
            listenerSocket = null;
        }

        private void DoAcceptClient(IAsyncResult result)
        {
            Socket socket = listenerSocket.EndAccept(result);

            byte asyncState = (byte)result.AsyncState;
            byte emptySlot = FindEmptySlot();

            Log($"Got new client to add for Player ID of {emptySlot} and async {asyncState}");
            Log($"Socket Info R:{socket.RemoteEndPoint} and L:{socket.LocalEndPoint}");

            if (clientLimit > 0 && emptySlot > clientLimit)
            {
                Log($"Out of bounds client number");
                socket.Disconnect(false);
                socket.Dispose();
                socket = null;
            }
            socketLookupTable.Add(emptySlot, socket);
            socketLookupTable[emptySlot].ReceiveBufferSize = NetworkStatics.bufferSize;
            socketLookupTable[emptySlot].SendBufferSize = NetworkStatics.bufferSize;

            BeginReceiveData(emptySlot);

            if (currentMaxPlayerID <= emptySlot)
                currentMaxPlayerID = emptySlot;

            Core.GismoThreading.ExecuteInNormalUpdate(() =>
            {
                onClientConnected?.Invoke(emptySlot);
            });

            if (!serverListening)
                return;
            listenerSocket.BeginAccept(new AsyncCallback(DoAcceptClient), asyncState);
        }

        public bool IsConnected()
        {
            if (listenerSocket == null || socketLookupTable == null)
                return false;

            return true;
        }

        private void BeginReceiveData(byte index)
        {
            PlayerConnection playerConnection = new PlayerConnection(index);
            socketLookupTable[index].BeginReceive(playerConnection.buffers.recieveBuffer, 0, NetworkStatics.bufferSize, SocketFlags.None, new AsyncCallback(DoReceive), playerConnection);
        }

        private void DoReceive(IAsyncResult result)
        {
            PlayerConnection playerConnection = (PlayerConnection)result.AsyncState;
            int socketLength;
            try
            {
                socketLength = socketLookupTable[playerConnection.playerIndex].EndReceive(result);
            }
            catch
            {
                Log($"Player {playerConnection.playerIndex} has error: ConnectionForciblyClosedException");
                Disconnect(playerConnection.playerIndex);
                playerConnection.Dispose();
                return;
            }

            if (!ValidClient(playerConnection.playerIndex))
            {
                Log($"Player { playerConnection.playerIndex} has error: Socket Error");
                playerConnection.Dispose();

                return;
            }

            if (socketLength == 0)
            {
                Log($"Player {playerConnection.playerIndex} has error: No Data was recieved!");
                Disconnect(playerConnection.playerIndex);
                playerConnection.Dispose();
            }
            else
            {
                playerConnection.buffers.CopyRecieveToPacket(socketLength);

                PacketHandler(playerConnection);

                playerConnection.buffers.ResetBuffers();

                try
                {
                    socketLookupTable[playerConnection.playerIndex].BeginReceive(playerConnection.buffers.recieveBuffer, 0, socketLookupTable[playerConnection.playerIndex].ReceiveBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), playerConnection);
                }
                catch
                {
                    Log($"Begin Recieve in Do Recieve has failed for {playerConnection.playerIndex}");
                }
            }
        }

        private void PacketHandler(PlayerConnection connection)
        {
            //Log($"Handling data! from {connection.playerIndex}, {connection.buffers.GetBufferSizes()}");

            if (connection.buffers.packetRing.Length < 4)
            {
                Log($"Net ERROR from {connection.playerIndex}: Packet isn't long enough to contain a packet ID");
                Disconnect(connection.playerIndex);
                return;
            }
            else
            {
                Core.Packet packet = new Core.Packet(connection.buffers.packetRing);

                NetworkPackets.ClientSentPackets packetID = (NetworkPackets.ClientSentPackets)Enum.ToObject(
                    typeof(NetworkPackets.ClientSentPackets), 
                    packet.ReadPacketID()
                    );
                Log($"Got packet of {packetID}");

                if (NetworkPackets.ServerFunctions.ContainsKey(packetID))
                {

                    byte cid = packet.ReadByte();
                    Core.GismoThreading.ExecuteInNormalUpdate(() =>
                    {
                        NetworkPackets.ServerFunctions[packetID].Invoke(packet, cid);

                        packet.Dispose();
                    });
                }
                else
                {
                    Log($"ServerFunctions functions doesn't have an entry for {packetID} for player {connection.playerIndex}");
                }
            }
        }

        public void SendDataTo(byte playerIndex, Core.Packet packet)
        {
            if (!ValidClient(playerIndex))
            {
                if (socketLookupTable[playerIndex] == null || !socketLookupTable[playerIndex].Connected)
                {
                    Disconnect(playerIndex);
                }
                return;
            }
            else
            {
#if !UNITY_EDITOR
                byte[] data = packet.ToArray();
                socketLookupTable[playerIndex].BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(DoSend), playerIndex);
#endif
            }
        }

        public void SendDataToAll(Core.Packet packet)
        {
            for (byte index = 0; index <= currentMaxPlayerID; index++)
            {
                if (socketLookupTable.ContainsKey(index))
                {
                    SendDataTo(index, packet);
                }
            }
        }

        public void SendDataToAllBut(int notPlayerIndex, Core.Packet packet)
        {
            for (byte playerID = 0; playerID <= currentMaxPlayerID; playerID++)
            {
                if (socketLookupTable.ContainsKey(playerID) && playerID != notPlayerIndex)
                {
                    SendDataTo(playerID, packet);
                }
            }
        }

        private void DoSend(IAsyncResult result)
        {
            byte asyncState = (byte)result.AsyncState;
            try
            {
                socketLookupTable[asyncState].EndSend(result);
            }
            catch
            {
                Log($"Player {asyncState} has error: ConnectionForciblyClosedException");
                Disconnect(asyncState);
            }
        }

        public bool IsPlayerIndexConnected(byte playerIndex)
        {
            if (!socketLookupTable.ContainsKey(playerIndex))
            {
                return false;
            }
            if (socketLookupTable[playerIndex].Connected)
            {
                return true;
            }
            return false;
        }

        public string GetIPv4()
        {
            foreach(IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
            return null;
        }

        public string ClientIp(byte index)
        {
            if (IsPlayerIndexConnected(index))
            {
                return ((IPEndPoint)socketLookupTable[index].RemoteEndPoint).ToString();
            }

            throw new Exception("Client index doesn't exist");
        }

        public void Disconnect(byte playerIndex, bool forcedDisconnect = false)
        {
            if (forcedDisconnect)
                Log($"Player {playerIndex} has been forced to disconnect");

            if (!ValidClient(playerIndex))
            {
                Log($"Player {playerIndex} lookup table and/or socket error");

                DisconnectUser(playerIndex);

                return;
            }
            socketLookupTable[playerIndex].BeginDisconnect(false, new AsyncCallback(DoDisconnect), playerIndex);
        }

        private void DoDisconnect(IAsyncResult result)
        {
            byte asyncState = (byte)result.AsyncState;
            try
            {
                socketLookupTable[asyncState].EndDisconnect(result);
            }
            catch
            {
                Log($"Do Disconnect failed for {asyncState}");
            }

            DisconnectUser(asyncState);
        }

        private void DisconnectUser(byte id)
        {
            if (!socketLookupTable.ContainsKey(id))
            {
                return;
            }

            socketLookupTable[id].Dispose();
            socketLookupTable[id] = null;
            socketLookupTable.Remove(id);
        }

        private byte FindEmptySlot()
        {
            byte empty = byte.MinValue;

            while (socketLookupTable.ContainsKey(empty))
            {
                empty++;
            }
            return empty;
        }

        private bool ValidClient(byte id)
        {
            if (!socketLookupTable.ContainsKey(id) || socketLookupTable[id] == null || !socketLookupTable[id].Connected)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            Log("Disposing of server");
            StopListening();
            foreach (byte key in socketLookupTable.Keys)
            {
                Disconnect(key);
            }
            socketLookupTable.Clear();
            socketLookupTable = null;
            playerIDs.Clear();
            playerIDs = null;
        }

        private struct PlayerConnection : IDisposable
        {
            internal byte playerIndex;
            internal ByteBuffers buffers;

            internal PlayerConnection(byte index)
            {
                playerIndex = index;
                buffers = new ByteBuffers
                {
                    recieveBuffer = new byte[NetworkStatics.bufferSize],
                    packetRing = null
                };
            }

            public void Dispose()
            {
                buffers.Dispose();
            }
        }
    }
}
