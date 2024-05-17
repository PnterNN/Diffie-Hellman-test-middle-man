using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackerClient.NET.IO
{
    public class PacketBuilder
    {
        MemoryStream _ms;
        public PacketBuilder()
        {
            _ms = new MemoryStream();
        }



        public void WriteMessage(string msg)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            _ms.Write(lengthBytes, 0, lengthBytes.Length);
            _ms.Write(messageBytes, 0, messageBytes.Length);
        }

        public void WriteOpCode(byte opcode)
        {
            _ms.WriteByte(opcode);
        }
        public byte[] GetPacketBytes()
        {
            var result = _ms.ToArray();
            return result;
        }
        public void WritePublicKey(byte[] publicKey)
        {
            byte[] lengthBytes = BitConverter.GetBytes(publicKey.Length);
            _ms.Write(lengthBytes, 0, lengthBytes.Length);
            _ms.Write(publicKey, 0, publicKey.Length);
        }
    }
}
