using System;
using System.Buffers;
using System.Collections.Generic;

namespace AviParserLib
{
    [Flags]
    public enum IndexFlag : uint
    {
        List = 0x00000001,
        Keyframe = 0x00000010,
        FirstPart = 0x00000020,
        LastPart = 0x00000040,
        NoTime = 0x00000100,
    }

    public class Index1Entry
    {
        public uint ChunkId { get; }
        public IndexFlag Flags { get; }
        public uint ChunkOffset { get; }
        public uint ChunkLength { get; }

        public string ChunkIdString => RiffAtom.FourCCToString(ChunkId);

        public bool IsKeyframe => Flags.HasFlag(IndexFlag.Keyframe);

        public Index1Entry(uint chunkId, IndexFlag flags, uint chunkOffset, uint chunkLength)
        {
            ChunkId = chunkId;
            Flags = flags;
            ChunkOffset = chunkOffset;
            ChunkLength = chunkLength;
        }
    }

    public class Index1Chunk : ChunkAtom
    {
        public new const uint FourCC = ((uint)(byte)('i') | ((uint)(byte)('d') << 8) | ((uint)(byte)('x') << 16) | ((uint)(byte)('1') << 24));

        public Index1Entry[] Entries { get; }

        public Index1Chunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            if (fourCC != FourCC)
            {
                throw new ArgumentException();
            }

            SeekData(aviFile.FileStream);
            int nEntries = (int)(Size / 16);

            var data = ArrayPool<byte>.Shared.Rent(nEntries * 16);
            File.FileStream.Read(data, 0, nEntries * 16);

            var entries = new List<Index1Entry>();
            for (int i = 0; i < nEntries; i++)
            {
                uint chunkId = BitConverter.ToUInt32(data, i * 16 + 0);
                IndexFlag flags = (IndexFlag)BitConverter.ToUInt32(data, i * 16 + 4);
                uint chunkOffset = BitConverter.ToUInt32(data, i * 16 + 8);
                uint chunkLength = BitConverter.ToUInt32(data, i * 16 + 12);
                entries.Add(new Index1Entry(chunkId, flags, chunkOffset, chunkLength));
            }

            Entries = entries.ToArray();

            ArrayPool<byte>.Shared.Return(data);
        }
    }
}


