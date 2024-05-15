using System;
using System.Buffers;
using System.Collections.Generic;

namespace AviParserLib
{
    public class StandardIndexEntry
    {
        public uint Offset { get; }
        public uint Size { get; }
        
        public bool IsKeyframe => (Size & (1 << 31)) == 0;

        public uint RealSize => (Size & ~(1 << 31));

        public StandardIndexEntry(uint offset, uint size)
        {
            Offset = offset;
            Size = size;
        }
    }

    public class OpenDmlStandardIndexChunk : OpenDmlIndexChunkBase
    {
        public ulong BaseOffset { get; }

        public StandardIndexEntry[] Entries { get; }

        public OpenDmlStandardIndexChunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            var data = ArrayPool<byte>.Shared.Rent(Math.Max(12, (int)EntriesInUse * 8));
            File.FileStream.Read(data, 0, 12);
            BaseOffset = BitConverter.ToUInt64(data, 0);

            File.FileStream.Read(data, 0, (int)EntriesInUse * 8);

            var entries = new List<StandardIndexEntry>();
            for (int i = 0; i < EntriesInUse; i++)
            {
                uint offset = BitConverter.ToUInt32(data, i * 8 + 0);
                uint chunkSize = BitConverter.ToUInt32(data, i * 8 + 4);
                entries.Add(new StandardIndexEntry(offset, chunkSize));
            }

            Entries = entries.ToArray();

            ArrayPool<byte>.Shared.Return(data);

            foreach (var entry in Entries)
            {
                aviFile.IndexedChunks.Add(new IndexedChunk((long)BaseOffset + entry.Offset, entry.RealSize, entry.IsKeyframe));
            }
        }
    }
}
