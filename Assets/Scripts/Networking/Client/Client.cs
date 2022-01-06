using System;
using System.Net;
using System.Net.Sockets;

namespace Gismo.Networking.Client
{
    public sealed class Client : IDisposable
    {
        private Socket selfSocket;
        private bool isConnecting;

        public int clientID;

        private readonly ByteBuffers buffers;

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
            Core.GismoThreading.InitalizeThreading();
            NetworkPackets.InitalizeFunctions();

            clientID = -1;

            NetworkPackets.ClientFunctions.Add(NetworkPackets.ServerSentPackets.FirstConnect, RecievePlayerID);

            if (selfSocket != null)
                return;
            buffers = new ByteBuffers();
            selfSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(string ip)
        {
            if (selfSocket == null || selfSocket.Connected || isConnecting)
                return;
            if (ip.ToLower() == "localhost")
            {
                selfSocket.BeginConnect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), NetworkStatics.portNumberTCP), new AsyncCallback(DoConnect), null);
            }
            else
            {
                selfSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(ip), NetworkStatics.portNumberTCP), new AsyncCallback(DoConnect), null);
            }

            DL.instance.Log($"Connected to ip {ip} with port {NetworkStatics.portNumberTCP}");
        }

        private void DoConnect(IAsyncResult result)
        {
            try
            {
                selfSocket.EndConnect(result);
            }
            catch
            {
                isConnecting = false;
                return;
            }
            if (!selfSocket.Connected)
            {
                isConnecting = false;
            }
            else
            {
                isConnecting = false;
                selfSocket.ReceiveBufferSize = NetworkStatics.bufferSize;
                selfSocket.SendBufferSize = NetworkStatics.bufferSize;
                BeginReceiveData();
            }
        }

        private void BeginReceiveData()
        {
            buffers.recieveBuffer = new byte[NetworkStatics.bufferSize];
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
                DL.instance.Log("Net ERROR: Socket error, possibly forced closure");
                Disconnect();
                return;
            }
            if (socketLength == 0)
            {
                if (selfSocket == null)
                    return;
                DL.instance.Log("Net ERROR: No data recieved!");
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
            DL.instance.Log($"{buffers.GetBufferSizes()}");
            if(buffers.packetRing.Length < 4)
            {
                DL.instance.Log("Net ERROR: Packet isn't long enough to contain a packet ID");
                Disconnect();
                return;
            }
            else
            {                 
                Core.Packet packet = new Core.Packet(buffers.packetRing);
                NetworkPackets.ServerSentPackets packetID = (NetworkPackets.ServerSentPackets)Enum.ToObject(typeof(NetworkPackets.ServerSentPackets), packet.ReadPacketID());
                DL.instance.Log($"Got packet of {packetID}");

                if(NetworkPackets.ClientFunctions.ContainsKey(packetID))
                {
                    NetworkPackets.ClientFunctions[packetID].Invoke(packet);
                }
                else
                {
                    DL.instance.Log($"Client functions doesn't have an entry for {packetID}");
                }
                return;
            }
        }

        public void SendData(Core.Packet packet)
        {
            if (!selfSocket.Connected)
            {
                DL.instance.Log($"Net ERROR: Socket isn't connected, cannot send data to disconnected socket");
                return;
            }

            if(clientID == -1)
            {
                DL.instance.Log("NET ERROR: Socket hasn't been assigned a player ID yet");
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
                DL.instance.Log("Net ERROR: ConnectionForciblyClosedException");
                Disconnect();
            }
        }

        public void RecievePlayerID(Core.Packet packet)
        {
            clientID = packet.ReadInt();
            DL.instance.Log($"Got a player ID of {clientID}");
        }

        public void Disconnect(bool forcedDisconnect = true)
        {
            if (selfSocket == null || !selfSocket.Connected)
                return;
            if(forcedDisconnect)
                DL.instance.Log($"Disconnecting");
            selfSocket.BeginDisconnect(false, new AsyncCallback(DoDisconnect), null);
        }

        private void DoDisconnect(IAsyncResult result)
        {
            try
            {
                selfSocket.EndDisconnect(result);
            }
            catch
            {}
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
