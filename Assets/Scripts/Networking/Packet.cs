using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Gismo.Networking.Core
{
    public struct Packet : IDisposable
    {
        public byte[] rawData;
        public int readIndex;

        public Packet(NetworkPackets.ClientSentPackets packetID, int playerID)
        {
            rawData = new byte[8];
            readIndex = 0;
            WriteInt((int)packetID);
            WriteInt(playerID);
        }

        public Packet(NetworkPackets.ServerSentPackets packetID)
        {
            rawData = new byte[4];
            readIndex = 0;
            WriteInt((int)packetID);
        }

        public Packet(byte[] bytes)
        {
            rawData = bytes;
            readIndex = 0;
        }

        public int ReadPacketID()
        {
            if(readIndex == 0)
            {
                return ReadInt();
            }
            throw new Exception($"Read index of packet {GetHashCode()} isn't at zero, must be at zero to read packetIndex");
        }

        public void Dispose()
        {
            rawData = null;
            readIndex = 0;
        }

        public byte[] ToArray()
        {
            byte[] returnable = new byte[readIndex];
            Buffer.BlockCopy(rawData, 0, returnable, 0, readIndex);
            return returnable;
        }

        private void CheckSize(int length)
        {
            int rawLength = rawData.Length;
            if (length + readIndex < rawLength)
            {
                return;
            }
            if (rawLength < 4)
            {
                rawLength = 4;
            }
            int doubleRawLength = rawLength * 2;
            while (length + readIndex >= doubleRawLength)
            {
                doubleRawLength *= 2;
            }
            byte[] bytes = new byte[doubleRawLength];
            Buffer.BlockCopy(rawData, 0, bytes, 0, readIndex);
            rawData = bytes;
        }

        public byte[] ReadBlock(int size, bool moveIndex = true)
        {
            if (size <= 0 || readIndex + size > rawData.Length)
            {
                return new byte[0];
            }
            byte[] resultArray = new byte[size];
            Buffer.BlockCopy(rawData, readIndex, resultArray, 0, size);
            if (moveIndex)
                readIndex += size;
            return resultArray;
        }

        public object ReadObject(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                return null;
            }
            int count = BitConverter.ToInt32(rawData, readIndex);
            if (moveIndex)
                readIndex += 4;
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                return null;
            }
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.SetLength(count);
            memoryStream.Read(rawData, readIndex, count);
            if (moveIndex)
                readIndex += count;
            object obj = new BinaryFormatter().Deserialize(memoryStream);
            memoryStream.Dispose();
            return obj;
        }

        public byte[] ReadBytes(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                return new byte[0];
            }
            int count = BitConverter.ToInt32(rawData, readIndex);
            if(moveIndex)
                readIndex += 4;
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                return new byte[0];
            }
            byte[] numArray = new byte[count];
            Buffer.BlockCopy(rawData, readIndex, numArray, 0, count);
            if (moveIndex)
                readIndex += count;
            return numArray;
        }

        public string ReadString(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                return "";
            }
            int count = BitConverter.ToInt32(rawData, readIndex);
            if (moveIndex)
                readIndex += 4;
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                return "";
            }
            string str = Encoding.UTF8.GetString(rawData, readIndex, count);
            if(moveIndex)
                readIndex += count;
            return str;
        }

        public char ReadChar(bool moveIndex = true)
        {
            if (readIndex + 2 > rawData.Length)
            {
                return char.MinValue;
            }
            char character = BitConverter.ToChar(rawData, readIndex);
            if(moveIndex)
                readIndex += 2;
            return character;
        }

        public byte ReadByte(bool moveIndex = true)
        {
            if (readIndex + 1 > rawData.Length)
            {
                return 0;
            }
            byte bit = rawData[readIndex];
            if(moveIndex)
                readIndex++;
            return bit;
        }

        public bool ReadBoolean(bool moveIndex = true)
        {
            if (readIndex + 1 > rawData.Length)
            {
                return false;
            }
            bool value = BitConverter.ToBoolean(rawData, readIndex);

            if (moveIndex)
                readIndex++;
            return value;
        }

        public int ReadInt(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                return 0;
            }
            int integer = BitConverter.ToInt32(rawData, readIndex);

            if(moveIndex)
                readIndex += 4;
            return integer;
        }

        public float ReadFloat(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                return 0.0f;
            }
            float value = BitConverter.ToSingle(rawData, readIndex);
            
            if(moveIndex)
                readIndex += 4;
            return value;
        }

        public void WriteBlock(byte[] bytes)
        {
            CheckSize(bytes.Length);
            Buffer.BlockCopy(bytes, 0,rawData, readIndex, bytes.Length);
            readIndex += bytes.Length;
        }

        public void WriteBlock(byte[] bytes, int offset, int size)
        {
            CheckSize(size);
            Buffer.BlockCopy(bytes, offset, rawData, readIndex, size);
            readIndex += size;
        }

        public void WriteObject(object value)
        {
            MemoryStream memoryStream = new MemoryStream();
            new BinaryFormatter().Serialize(memoryStream, value);
            byte[] array = memoryStream.ToArray();
            int length = array.Length;
            memoryStream.Dispose();
            WriteBlock(BitConverter.GetBytes(length));
            WriteBlock(array);
        }

        public void WriteBytes(byte[] value, int offset, int size)
        {
            WriteBlock(BitConverter.GetBytes(size));
            WriteBlock(value, offset, size);
        }

        public void WriteBytes(byte[] value)
        {
            WriteBlock(BitConverter.GetBytes(value.Length));
            WriteBlock(value);
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt(0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                WriteInt(bytes.Length);
                WriteBlock(bytes);
            }
        }

        public void WriteChar(char value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteByte(byte value)
        {
            CheckSize(1);
            rawData[readIndex] = value;
            readIndex++;
        }

        public void WriteBoolean(bool value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteInt(int value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteFloat(float value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }
    }
}
