using System.Collections;
using System.Collections.Generic;
using System;

namespace J2.Networking
{
    public class PacketManager
    {
        public static PacketManager Instance { get; } = new PacketManager();

        public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
        {
            GamePacketHandler.HandlePacket(session, buffer);
        }
    }
}
