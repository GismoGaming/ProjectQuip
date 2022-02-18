using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Gismo.Networking.Core
{
    public class NetTransform
    {
        public UnityEngine.Vector3 position;
        public UnityEngine.Quaternion rotation;
        public UnityEngine.Vector3 scale;

        public override string ToString()
        {
            return $"{position} : {rotation.eulerAngles} : {scale}";
        }
    }
    
    public struct Packet : IDisposable
    {
        public byte[] rawData;
        public int readIndex;

        public DateTime timeStamp;

        public string GetByteString()
        {
            return rawData.GetString();
        }

        public Packet(NetworkPackets.ClientSentPackets packetID, int playerID)
        {
            rawData = new byte[8];
            readIndex = 0;
            timeStamp = DateTime.Now;

            WriteInt((int)packetID);
            WriteInt(playerID);
        }

        public Packet(NetworkPackets.ServerSentPackets packetID)
        {
            rawData = new byte[4];
            readIndex = 0;
            timeStamp = DateTime.Now;

            WriteInt((int)packetID);
        }

        public Packet(byte[] bytes)
        {
            rawData = bytes;
            readIndex = 0;
            timeStamp = DateTime.Now;
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

        public byte[] ToArray(bool shorten = true)
        {
            if (shorten)
            {
                byte[] returnable = new byte[readIndex];
                Buffer.BlockCopy(rawData, 0, returnable, 0, readIndex);

                return rawData;
            }
            return rawData;
        }

        public byte[] GetData(bool isClient)
        {
            List<byte> temp = new List<byte>(rawData);
            if (isClient)
                temp.RemoveRange(0, 8);
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

        public object ReadObject(bool moveIndex = true)
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
                throw new Exception("Not enough space to read this type in the packet");
            }
            int count = BitConverter.ToInt32(rawData, readIndex);
            if(moveIndex)
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
            string str = Encoding.UTF8.GetString(rawData, readIndex, count);
            if(moveIndex)
                readIndex += count;
            return str;
        }

        public char ReadChar(bool moveIndex = true)
        {
            if (readIndex + 2 > rawData.Length)
            {
                throw new Exception("Not enough space to read this type in the packet");
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
                throw new Exception("Not enough space to read this type in the packet");
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
                throw new Exception("Not enough space to read this type in the packet");
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
                throw new Exception("Not enough space to read this type in the packet");
            }
            float value = BitConverter.ToSingle(rawData, readIndex);
            
            if(moveIndex)
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
                UnityEngine.Vector2 val = new UnityEngine.Vector3(ReadFloat(), ReadFloat());
                readIndex = prevIndex;
                return val;
            }
        }

        public NetTransform ReadTransform(bool moveIndex = true)
        {
            if(moveIndex)
            {
                return new NetTransform
                {
                    position = ReadVector3(moveIndex),
                    rotation = UnityEngine.Quaternion.Euler(ReadVector3(moveIndex)),
                    scale = ReadVector3(moveIndex)
                };
            }
            else
            {
                int prevIndex = readIndex;
                NetTransform t = new NetTransform
                {
                    position = ReadVector3(moveIndex),
                    rotation = UnityEngine.Quaternion.Euler(ReadVector3(moveIndex)),
                    scale = ReadVector3(moveIndex)
                };
                readIndex = prevIndex;
                return t;
            }
        }

        #endregion

        #region Write
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
                throw new Exception("Please insert a value");
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
        #endregion
    }
}
