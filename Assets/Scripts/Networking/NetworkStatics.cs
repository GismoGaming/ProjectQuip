using System;
using System.Collections.Generic;

namespace Gismo.Networking
{
    internal class ByteBuffers : IDisposable
    {
        public byte[] recieveBuffer;
        public byte[] packetRing;

        public void ResetBuffers()
        {
            recieveBuffer = new byte[NetworkStatics.bufferSize];
            packetRing = new byte[NetworkStatics.bufferSize];
        }

        public string GetBufferSizes()
        {
            return $"recieve: {recieveBuffer.Length}\t ring: {packetRing.Length}";
        }

        public string GetBufferContents()
        {
            return $"recieve: {NetworkStatics.ByteToString(recieveBuffer)}\t ring: {NetworkStatics.ByteToString(packetRing)}";
        }

        public void CopyRecieveToPacket(int socketLength)
        {
            packetRing = new byte[socketLength];
            Buffer.BlockCopy(recieveBuffer, 0, packetRing, 0, socketLength);
        }

        public void Dispose()
        {
            recieveBuffer = null;
            packetRing = null;
        }
    }
    public class NetworkStatics
    {
        public static int bufferSize = 128;

        public static string ByteToString(byte[] bytes)
        {

            string returnable = "";
            foreach(byte b in bytes)
            {
                returnable += b.ToString();
            }

            return returnable;
        }

        public const int portNumberTCP = 1173;
        public const int portNumberUDP = 1174;

        public const int maxServerConnections = 3;
    }

    public class NetworkPackets
    {
        // Sent to the server from clients
        public enum ClientSentPackets { MSGSend, ClientPosition};

        // Sent to a specific client from the server
        public enum ServerSentPackets { FirstConnect ,MSGSend, ClientPositionShare, PlayerDictionaryShare };

        /// <summary>
        /// Server Function Dictionary that contains all functions that the server executes upon 
        /// Recieving a packet from a client
        /// </summary>
        public static Dictionary<ClientSentPackets, ClientPacketRecieved> ServerFunctions;

        /// <summary>
        /// Client Function Dictionary that contains all functions that the client executes upon 
        /// Recieving a packet from the server
        /// </summary>
        public static Dictionary<ServerSentPackets, ServerPacketRecieved> ClientFunctions;
        public delegate void ClientPacketRecieved(Core.Packet packet, int playerID);
        public delegate void ServerPacketRecieved(Core.Packet packet);

        static bool isInitalized;

        public static void InitalizeFunctions(bool forced = true)
        {
            if (isInitalized && !forced)
                return;

            ClientFunctions = new Dictionary<ServerSentPackets, ServerPacketRecieved>();
            ServerFunctions = new Dictionary<ClientSentPackets, ClientPacketRecieved>();

            isInitalized = true;
        }
    }
}
