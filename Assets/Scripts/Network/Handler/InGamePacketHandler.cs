using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using J2.Protocol;

namespace J2.Networking
{
    public static class InGamePacketHandler
    {
        public static void Handle_S_Spawn(PacketSession session, S_Spawn packet)
        {
            if (session is ServerSession server) server.EnqueueSpawn(packet);
        }

        public static void Handle_S_Move(PacketSession session, S_Move packet)
        {
        // TODO: Implement packet processing here. This body is preserved on regeneration.
        }

        public static void Handle_S_RequestResult(PacketSession session, S_RequestResult packet)
        {
        // TODO: Implement packet processing here. This body is preserved on regeneration.
        }
    // <packet-handler-stubs>
    }
}
