using System;
using System.Buffers;

namespace AviParserLib
{
    public class AviExtHeaderChunk : ChunkAtom
    {
        // ReSharper disable once InconsistentNaming
        public new const uint FourCC = ((uint)(byte)('d') | ((uint)(byte)('m') << 8) | ((uint)(byte)('l') << 16) | ((uint)(byte)('h') << 24));

        public uint GrandFrames { get; }

        public AviExtHeaderChunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            if (fourCC != FourCC)
            {
                throw new ArgumentException();
            }

            SeekData(aviFile.FileStream);

            var buffer = ArrayPool<byte>.Shared.Rent(1 * 4);
            aviFile.FileStream.Read(buffer, 0, 1 * 4);
            GrandFrames = BitConverter.ToUInt32(buffer, 0 * 4);
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
