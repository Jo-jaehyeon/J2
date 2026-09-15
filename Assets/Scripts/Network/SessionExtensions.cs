using System;
using Google.Protobuf;
using J2.Protocol;

namespace J2.Networking
{
    public static class SessionExtensions
    {
        public static void Send(this PacketSession session, IMessage packet)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (packet == null) throw new ArgumentNullException(nameof(packet));
            if (!packet.Descriptor.Name.StartsWith("C_", StringComparison.Ordinal))
                throw new InvalidOperationException("Wrong-direction outgoing packet type.");
            session.Send(new ArraySegment<byte>(PacketCodec.Encode(PacketCodec.GetId(packet), packet)));
        }
    }
}
