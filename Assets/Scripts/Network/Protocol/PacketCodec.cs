using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace J2.Protocol
{
    public static class PacketCodec
    {
        public const int Version = 1;
        public const int HeaderSize = 4;
        public const int MaxFrameSize = ushort.MaxValue;

        // Read the original proto enum names: generated C# enum names may have different casing.
        private static readonly Dictionary<string, PacketId> MessageIds = BuildMessageIds();

        private static Dictionary<string, PacketId> BuildMessageIds()
        {
            var result = new Dictionary<string, PacketId>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in GameEnumReflection.Descriptor.EnumTypes)
            {
                if (definition.Name != nameof(PacketId)) continue;
                foreach (var value in definition.Values)
                {
                    if (value.Number == 0) continue;
                    string name = value.Name.StartsWith("PKT_", StringComparison.Ordinal)
                        ? value.Name.Substring(4) : value.Name;
                    result.Add(name, (PacketId)value.Number);
                }
            }
            return result;
        }

        public static PacketId GetId(IMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (!MessageIds.TryGetValue(message.Descriptor.Name, out PacketId id))
                throw new InvalidOperationException("Message has no ID in GameEnum.proto.");
            return id;
        }
        // [total size: uint16 LE][packet ID: uint16 LE][protobuf body]
        public static byte[] Encode(PacketId id, IMessage message)
        {
            byte[] body = message.ToByteArray();
            int size = checked(HeaderSize + body.Length);
            if (size > MaxFrameSize) throw new InvalidDataException("Packet too large.");
            byte[] frame = new byte[size];
            frame[0] = (byte)size; frame[1] = (byte)(size >> 8);
            frame[2] = (byte)id; frame[3] = (byte)((int)id >> 8);
            Buffer.BlockCopy(body, 0, frame, HeaderSize, body.Length);
            return frame;
        }

        public static PacketId GetId(ArraySegment<byte> frame)
        {
            if (frame.Array == null || frame.Count < HeaderSize ||
                (frame.Array[frame.Offset] | frame.Array[frame.Offset + 1] << 8) != frame.Count)
                throw new InvalidDataException("Invalid packet length.");
            return (PacketId)(frame.Array[frame.Offset + 2] | frame.Array[frame.Offset + 3] << 8);
        }

        public static T Parse<T>(MessageParser<T> parser, ArraySegment<byte> frame) where T : IMessage<T>
        {
            GetId(frame);
            return parser.ParseFrom(frame.Array, frame.Offset + HeaderSize, frame.Count - HeaderSize);
        }

        public static async Task<byte[]> ReadAsync(Stream stream, CancellationToken token)
        {
            byte[] header = new byte[2];
            if (!await ReadExactlyAsync(stream, header, 0, 2, token).ConfigureAwait(false)) return null;
            int size = header[0] | header[1] << 8;
            if (size < HeaderSize) throw new InvalidDataException("Invalid packet length.");
            byte[] frame = new byte[size];
            Buffer.BlockCopy(header, 0, frame, 0, 2);
            if (!await ReadExactlyAsync(stream, frame, 2, size - 2, token).ConfigureAwait(false))
                throw new EndOfStreamException("Truncated packet.");
            return frame;
        }

        private static async Task<bool> ReadExactlyAsync(Stream stream, byte[] bytes, int offset, int count, CancellationToken token)
        {
            int read = 0;
            while (read < count)
            {
                int n = await stream.ReadAsync(bytes, offset + read, count - read, token).ConfigureAwait(false);
                if (n == 0)
                {
                    if (read != 0) throw new EndOfStreamException("Truncated packet.");
                    return false;
                }
                read += n;
            }
            return true;
        }
    }
}

