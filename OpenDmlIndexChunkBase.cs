using System;
using System.Buffers;

namespace AviParserLib
{
    public abstract class OpenDmlIndexChunkBase : ChunkAtom
    {
        public new const uint FourCC = ((uint)(byte)('i') | ((uint)(byte)('n') << 8) | ((uint)(byte)('d') << 16) | ((uint)(byte)('x') << 24));

        public const byte IndexOfIndexes = 0x00;
        public const byte IndexOfChunks = 0x01;
        public const byte IndexIsData = 0x80;

        public const byte Index2Field = 0x01;

        public ushort LongsPerEntry { get; }
        public byte IndexSubType { get; }
        public byte IndexType { get; }
        public uint EntriesInUse { get; }
        public uint ChunkId { get; }

        public string ChunkIdString => RiffAtom.FourCCToString(ChunkId);

        protected OpenDmlIndexChunkBase(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            SeekData(aviFile.FileStream);

            var data = ArrayPool<byte>.Shared.Rent(12);
            File.FileStream.Read(data, 0, 12);

            LongsPerEntry = BitConverter.ToUInt16(data, 0);
            IndexSubType = data[2];
            IndexType = data[3];
            EntriesInUse = BitConverter.ToUInt32(data, 4);
            ChunkId = BitConverter.ToUInt32(data, 8);

            ArrayPool<byte>.Shared.Return(data);
        }
    }
}
