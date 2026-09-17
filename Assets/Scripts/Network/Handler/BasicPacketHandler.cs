using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using J2.Protocol;

namespace J2.Networking
{
    public static class BasicPacketHandler
    {
        public static void Handle_S_Pong(PacketSession session, S_Pong packet)
        {
        // TODO: Implement packet processing here. This body is preserved on regeneration.
        }

        public static void Handle_S_EnterGame(PacketSession session, S_EnterGame packet)
        {
            if (session is ServerSession server)
            {
                server.SetEnteredGame(packet.SessionId, packet.ObjectId);
            }
        }

        public static void Handle_S_LeaveGame(PacketSession session, S_LeaveGame packet)
        {
        // TODO: Implement packet processing here. This body is preserved on regeneration.
        }

        public static void Handle_S_FindMatch(PacketSession session, S_FindMatch packet)
        {
            if (session is ServerSession server)
            {
                server.SetMatchedGame(packet.GameId);
            }
        }

        public static void Handle_S_AcceptMatch(PacketSession session, S_AcceptMatch packet)
        {
        // TODO: Implement packet processing here. This body is preserved on regeneration.
        }
    // <packet-handler-stubs>
    }
}
