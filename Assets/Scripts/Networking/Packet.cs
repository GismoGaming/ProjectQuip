using Gismo.Quip;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Gismo.Networking.Core
{
    public struct Packet : IDisposable
    {
        public byte[] rawData;
        public int readIndex;

        public string GetByteString()
        {
            return rawData.GetString();
        }

        public Packet(NetworkPackets.ClientSentPackets packetID, byte playerID)
        {
            rawData = new byte[5];
            readIndex = 0;

            WriteInt((int)packetID);
            WriteByte(playerID);
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
            if (readIndex == 0)
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

        public byte[] ToArray(bool shorten = true)
        {
            if (shorten && readIndex != 0)
            {
                byte[] returnable = new byte[readIndex];
                Buffer.BlockCopy(rawData, 0, returnable, 0, readIndex);

                return rawData;
            }
            return rawData;
        }

        public Packet CopyTo(Packet destPacket, bool isClient)
        {
            destPacket.WriteBlock(GetData(isClient));
            return destPacket;
        }

        public byte[] GetData(bool isClient)
        {
            List<byte> temp = new List<byte>(rawData);
            if (isClient)
                temp.RemoveRange(0, 5);
            else
                temp.RemoveRange(0, 4);

            return temp.ToArray();
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

            if (doubleRawLength > NetworkStatics.bufferSize)
                throw new Exception($"Packet is larger than the read/ recived buffers");

            byte[] bytes = new byte[doubleRawLength];
            Buffer.BlockCopy(rawData, 0, bytes, 0, readIndex);
            rawData = bytes;
        }
        #region Read
        public byte[] ReadBlock(int size, bool moveIndex = true)
        {
            if (size <= 0 || readIndex + size > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            byte[] resultArray = new byte[size];
            Buffer.BlockCopy(rawData, readIndex, resultArray, 0, size);
            if (moveIndex)
                readIndex += size;
            return resultArray;
        }

        object ReadObject(bool moveIndex = true)
        {
            int count = ReadInt();
            if (moveIndex)
                readIndex += 4;
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                throw new Exception($"Not enough space to read this type in the packet with it's data {readIndex} + {count} > {rawData.Length}");
            }
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(rawData, readIndex, count);
                if (moveIndex)
                    readIndex += count;
                memoryStream.Seek(0, SeekOrigin.Begin);
                object obj = new BinaryFormatter().Deserialize(memoryStream);
                return obj;
            }
        }

        public byte[] ReadBytes(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            int count = BitConverter.ToInt32(rawData, readIndex);
            if (moveIndex)
                readIndex += 4;
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            byte[] numArray = new byte[count];
            Buffer.BlockCopy(rawData, readIndex, numArray, 0, count);
            if (moveIndex)
                readIndex += count;
            return numArray;
        }

        public string ReadString(bool moveIndex = true)
        {
            int count = ReadInt(moveIndex);
            if (count <= 0 || readIndex + count > rawData.Length)
            {
                throw new Exception($"Not enough space to read this type in the packet {count}");
            }
            string str = Encoding.UTF8.GetString(rawData, readIndex, count);
            if (moveIndex)
                readIndex += count;

            if (str == "<[NULL]>")
                return null;

            return str;
        }

        public char ReadChar(bool moveIndex = true)
        {
            if (readIndex + 2 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            char character = BitConverter.ToChar(rawData, readIndex);
            if (moveIndex)
                readIndex += 2;
            return character;
        }

        public byte ReadByte(bool moveIndex = true)
        {
            if (readIndex + 1 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            byte bit = rawData[readIndex];
            if (moveIndex)
                readIndex++;
            return bit;
        }

        public bool ReadBoolean(bool moveIndex = true)
        {
            if (readIndex + 1 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
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
                throw new Exception($"Not enough space to read this type in the packet {readIndex} + 4 > {rawData.Length}");
            }
            int integer = BitConverter.ToInt32(rawData, readIndex);

            if (moveIndex)
                readIndex += 4;
            return integer;
        }

        public uint ReadUint(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            uint integer = BitConverter.ToUInt32(rawData, readIndex);

            if (moveIndex)
                readIndex += 4;
            return integer;
        }

        public float ReadFloat(bool moveIndex = true)
        {
            if (readIndex + 4 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
            }
            float value = BitConverter.ToSingle(rawData, readIndex);

            if (moveIndex)
                readIndex += 4;
            return value;
        }

        public UnityEngine.Vector3 ReadVector3(bool moveIndex = true)
        {
            if (moveIndex)
            {
                return new UnityEngine.Vector3(ReadFloat(), ReadFloat(), ReadFloat());
            }
            else
            {
                int prevIndex = readIndex;
                UnityEngine.Vector3 val = new UnityEngine.Vector3(ReadFloat(), ReadFloat(), ReadFloat());
                readIndex = prevIndex;
                return val;
            }
        }

        public UnityEngine.Vector2 ReadVector2(bool moveIndex = true)
        {
            if (moveIndex)
            {
                return new UnityEngine.Vector2(ReadFloat(), ReadFloat());
            }
            else
            {
                int prevIndex = readIndex;
                UnityEngine.Vector2 val = new UnityEngine.Vector2(ReadFloat(), ReadFloat());
                readIndex = prevIndex;
                return val;
            }
        }

        public T ReadGeneric<T>()
        {
            return (T)ReadObject();
        }

        public Quip.Minerals.MineralCostDetails ReadCostDetails()
        {
            return new Quip.Minerals.MineralCostDetails()
            {
                name = ReadString(),
                price = ReadFloat()
            };
        }

        public List<T> ReadList<T>()
        {
            int count = ReadInt();
            List<T> result = new List<T>();

            for (int i = 0; i < count; i++)
            {
                result.Add(ReadGeneric<T>());
            }
            return result;
        }

        public List<Tracked2DPositionByted> ReadListTracked2DPositionByted()
        {
            int count = ReadInt();
            List<Tracked2DPositionByted> result = new List<Tracked2DPositionByted>();

            for (int i = 0; i < count; i++)
            {
                result.Add(ReadTracked2DPositionByted());
            }
            return result;
        }

        public Tracked2DPositionByted ReadTracked2DPositionByted()
        {
            return new Tracked2DPositionByted
            {
                id = ReadByte(),
                position = ReadVector2()
            };
        }

        public List<PlayerDictionaryElement> ReadListPlayerDictionaryElement()
        {
            int count = ReadInt();
            List<PlayerDictionaryElement> result = new List<PlayerDictionaryElement>();

            for (int i = 0; i < count; i++)
            {
                result.Add(ReadPlayerDictionaryElement());
            }
            return result;
        }

        public List<uint> ReadUintList()
        {
            int count = ReadInt();
            List<uint> result = new List<uint>();

            for (int i = 0; i < count; i++)
            {
                result.Add(ReadUint());
            }
            return result;
        }

        public PlayerDictionaryElement ReadPlayerDictionaryElement()
        {
            return new PlayerDictionaryElement
            {
                id = ReadByte(),
                position = ReadVector2(),
                role = ReadInt(),
                username = ReadString(),
            };
        }

        #endregion

        #region Write
        public void WriteBlock(byte[] bytes)
        {
            CheckSize(bytes.Length);
            Buffer.BlockCopy(bytes, 0, rawData, readIndex, bytes.Length);
            readIndex += bytes.Length;
        }

        public void WriteBlock(byte[] bytes, int offset, int size)
        {
            CheckSize(size);
            Buffer.BlockCopy(bytes, offset, rawData, readIndex, size);
            readIndex += size;
        }

        void WriteObject(object value)
        {
            byte[] array;
            int length;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, value);

                array = memoryStream.ToArray();
                length = array.Length;
            }
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
                value = "<[NULL]>";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            WriteInt(bytes.Length);
            WriteBlock(bytes);
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

        public void WriteUint(uint value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteFloat(float value)
        {
            WriteBlock(BitConverter.GetBytes(value));
        }

        public void WriteVector3(UnityEngine.Vector3 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
            WriteFloat(value.z);
        }

        public void WriteVector2(UnityEngine.Vector2 value)
        {
            WriteFloat(value.x);
            WriteFloat(value.y);
        }

        public void WriteTransform(UnityEngine.Transform transform)
        {
            WriteVector3(transform.position);
            WriteVector3(transform.rotation.eulerAngles);
            WriteVector3(transform.localScale);
        }

        public void WriteGeneric<T>(T item)
        {
            WriteObject(item);
        }
        public void WriteCostDetails(Quip.Minerals.MineralCostDetails item)
        {
            WriteString(item.name);
            WriteFloat(item.price);
        }

        public void WriteList<T>(List<T> list)
        {
            WriteInt(list.Count);

            foreach (T item in list)
            {
                WriteGeneric(item);
            }
        }
        
        public void WriteList(List<uint> items)
        {
            WriteInt(items.Count);

            foreach (uint u in items)
            {
                WriteUint(u);
            }
        }
        public void WriteList(List<PlayerDictionaryElement> items)
        {
            WriteInt(items.Count);

            foreach (PlayerDictionaryElement e in items)
            {
                WritePlayerDictionaryElement(e);
            }
        }

        public void WritePlayerDictionaryElement(PlayerDictionaryElement element)
        {
            WriteByte(element.id);
            WriteVector2(element.position);
            WriteInt(element.role);

            WriteString(element.username);
        }

        public void WriteList(List<Tracked2DPositionByted> items)
        {
            WriteInt(items.Count);

            foreach (Tracked2DPositionByted e in items)
            {
                WriteTracked2DPositionByted(e);
            }
        }

        public void WriteTracked2DPositionByted(Tracked2DPositionByted element)
        {
            WriteByte(element.id);
            WriteVector2(element.position);
        }
        #endregion
    }
}