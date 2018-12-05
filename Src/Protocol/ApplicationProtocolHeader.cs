using System;
using System.Collections.Generic;
using System.Text;

namespace TcpServerBaseLibrary
{

    public struct ApplicationProtocolHeader
    {
        public int Lenght;

        public int MessageTypeIdentifier;


        public ApplicationProtocolHeader(int lenght, int msgtype)
        {
            Lenght = lenght;
            MessageTypeIdentifier = msgtype;
        }

        public ApplicationProtocolHeader(byte[] data)
        {
            this.Lenght = BitConverter.ToInt32(data, 0);
            this.MessageTypeIdentifier = BitConverter.ToInt32(data, 4);
        }

        public byte[] WrapHeaderData()
        {
            byte[] header = new byte[8];

            BitConverter.GetBytes(Lenght).CopyTo(header, 0);

            BitConverter.GetBytes((int)MessageTypeIdentifier).CopyTo(header, 4);

            return header;
        }
    }
}
