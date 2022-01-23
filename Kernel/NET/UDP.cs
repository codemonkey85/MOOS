// Copyright (c) MOSA Project. Licensed under the New BSD License.
using Kernel.NET;
using System.Runtime.InteropServices;

namespace Kernel
{
    public static unsafe class UDP
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UDPHeader
        {
            public ushort SrcPort;
            public ushort DestPort;
            public ushort Length;
            public ushort Checksum;
        }

        public static void SendPacket(byte[] DestIP, ushort SourcePort, ushort DestPort, byte[] Data)
        {
            int PacketLen = (sizeof(UDPHeader) + Data.Length);
            byte* Buffer = (byte*)Platform.kmalloc((ulong)PacketLen);
            UDPHeader* header = (UDPHeader*)Buffer;
            Native.Stosb(header, 0, (ulong)PacketLen);
            header->SrcPort = Ethernet.SwapLeftRight(SourcePort);
            header->DestPort = Ethernet.SwapLeftRight(DestPort);
            header->Length = Ethernet.SwapLeftRight(((ushort)PacketLen));
            header->Checksum = 0;
            for (int i = 0; i < Data.Length; i++) (Buffer + sizeof(UDPHeader))[i] = Data[i];

            IPv4.SendPacket(DestIP, 17, Buffer, PacketLen);

            Console.WriteLine("UDP Packet Sent");
        }

        internal static void HandlePacket(byte* frame, ushort length)
        {
            UDPHeader* header = (UDPHeader*)frame;
            Console.WriteLine("UDP Packet Received");
            frame += sizeof(UDPHeader);
            length -= (ushort)sizeof(UDPHeader);

            byte[] Buffer = new byte[length];
            fixed (byte* P = Buffer)
            {
                Native.Movsb(P, frame, length);
            }
            Buffer.Dispose();
        }
    }
}

