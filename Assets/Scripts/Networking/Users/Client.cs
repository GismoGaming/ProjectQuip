using System;
using System.Net;
using System.Net.Sockets;

using static DL;

namespace Gismo.Networking.Users
{
    public sealed class Client : IDisposable
    {
        private Socket selfSocket;
        private bool isConnecting;

        public byte clientID;

        private readonly ByteBuffers buffers;

        public static ClientFunction onClientConnected;

        public bool IsConnected
        {
            get
            {
                if (selfSocket != null)
                    return selfSocket.Connected;
                return false;
            }
        }

        public Client()
        {
            NetworkPackets.InitalizeFunctions();

            clientID = byte.MaxValue;

            NetworkPackets.ClientFunctions.Add(NetworkPackets.ServerSentPackets.FirstConnect, RecievePlayerID);

            if (selfSocket != null)
                return;
            buffers = new ByteBuffers();
            selfSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(string ip)
        {
            if (selfSocket == null || selfSocket.Connected || isConnecting)
            {
                Log("Connection initalizeation has failed");
                return;
            }

            if (ip.ToLower() == "localhost")
            {
                selfSocket.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), NetworkStatics.portNumberTCP), new AsyncCallback(DoConnect), null);
            }
            else
            {
                selfSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), NetworkStatics.portNumberTCP), new AsyncCallback(DoConnect), null);
            }

            Log($"Connected to ip {ip} with port {NetworkStatics.portNumberTCP}");
        }

        private void DoConnect(IAsyncResult result)
        {
            isConnecting = false;
            try
            {
                selfSocket.EndConnect(result);
            }
            catch
            {
                Log("Do Connect in Client Faililed");
                return;
            }

            if (selfSocket.Connected)
            {
                selfSocket.ReceiveBufferSize = NetworkStatics.bufferSize;
                selfSocket.SendBufferSize = NetworkStatics.bufferSize;
                BeginReceiveData();
            }
        }

        private void BeginReceiveData()
        {
            buffers.ResetBuffers();
            selfSocket.BeginReceive(buffers.recieveBuffer, 0, selfSocket.ReceiveBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), null);
        }

        private void DoReceive(IAsyncResult result)
        {
            int socketLength;
            try
            {
                socketLength = selfSocket.EndReceive(result);
            }
            catch
            {
                Log("Net ERROR: Socket error, possibly forced closure");
                Disconnect();
                return;
            }

            if (socketLength == 0)
            {
                if (selfSocket == null)
                {
                    Log("No Socket in DoRecieve");
                    return;
                }
                Log("Net ERROR: No data recieved!");
                Disconnect();
            }
            else
            {
                buffers.CopyRecieveToPacket(socketLength);
                PacketHandler();
                buffers.ResetBuffers();
                selfSocket.BeginReceive(buffers.recieveBuffer, 0, selfSocket.ReceiveBufferSize, SocketFlags.None, new AsyncCallback(DoReceive), null);
            }
        }

        private void PacketHandler()
        {
            //Log($"{buffers.GetBufferSizes()}");
            if (buffers.packetRing.Length < 4)
            {
                Log("Net ERROR: Packet isn't long enough to contain a packet ID");
                Disconnect();
                return;
            }
            else
            {
                Core.Packet packet = new Core.Packet(buffers.packetRing);
                NetworkPackets.ServerSentPackets packetID = (NetworkPackets.ServerSentPackets)Enum.ToObject(typeof(NetworkPackets.ServerSentPackets), packet.ReadPacketID());
                
                Log($"Got packet of {packetID}");

                if (NetworkPackets.ClientFunctions.ContainsKey(packetID))
                {
                    Core.GismoThreading.ExecuteInNormalUpdate( ()=>
                    {
                        NetworkPackets.ClientFunctions[packetID].Invoke(packet);

                        packet.Dispose();
                    });
                }
                else
                {
                    Log($"Client functions doesn't have an entry for {packetID}");
                }
                return;
            }
        }

        public void SendData(Core.Packet packet)
        {
            if (!selfSocket.Connected)
            {
                Log($"Net ERROR: Socket isn't connected, cannot send data to disconnected socket");
                return;
            }

            if (clientID == byte.MaxValue)
            {
                Log("NET ERROR: Socket hasn't been assigned a player ID yet");
                return;
            }

            byte[] data = packet.ToArray();
            selfSocket?.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(DoSend), null);
        }

        private void DoSend(IAsyncResult ar)
        {
            try
            {
                selfSocket.EndSend(ar);
            }
            catch
            {
                Log("Net ERROR: ConnectionForciblyClosedException");
                Disconnect();
            }
        }

        public void RecievePlayerID(Core.Packet packet)
        {
            if (clientID == byte.MaxValue)
            {
                clientID = packet.ReadByte();
                Log($"Got a player ID of {clientID}");

                onClientConnected?.Invoke(clientID);
            }
            else
            {
                Log($"Got duplicate player id packet of {packet.ReadInt()}, even though we have been set up correctly | {clientID} | {packet.timeStamp.Second} ");
            }
        }

        public void Disconnect(bool forcedDisconnect = true)
        {
            if (selfSocket == null || !selfSocket.Connected)
            {
                Log("Socket isn't set up correctly");
                return;
            }
            if (forcedDisconnect)
                Log($"Disconnecting");

            Log($"Beggining Disconnecting");
            selfSocket.BeginDisconnect(false, new AsyncCallback(DoDisconnect), null);
        }

        private void DoDisconnect(IAsyncResult result)
        {
            try
            {
                selfSocket.EndDisconnect(result);
            }
            catch
            {
                Log($"Do Disconnect for {result} has failed");
            }
        }

        public void Dispose()
        {
            Disconnect();
            buffers.Dispose();
            selfSocket.Close();
            selfSocket.Dispose();
            selfSocket = null;
        }
    }
}
