using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace AviParserLib
{
    public class SuperIndexEntry
    {
        public ulong Offset { get; }
        public uint Size { get; }
        public uint Duration { get; }

        public OpenDmlStandardIndexChunk Index { get; internal set; }

        public SuperIndexEntry(ulong offset, uint size, uint duration)
        {
            Offset = offset;
            Size = size;
            Duration = duration;
        }
    }

    public class OpenDmlSuperIndexChunk : OpenDmlIndexChunkBase
    {
        public bool IsIndex2Field => IndexSubType == Index2Field;

        public SuperIndexEntry[] Entries { get; }

        public OpenDmlSuperIndexChunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            aviFile.FileStream.Seek(12, SeekOrigin.Current);

            var data = ArrayPool<byte>.Shared.Rent((int)EntriesInUse * 16);
            File.FileStream.Read(data, 0, (int)EntriesInUse * 16);

            var entries = new List<SuperIndexEntry>();
            for (int i = 0; i < EntriesInUse; i++)
            {
                ulong offset = BitConverter.ToUInt64(data, i * 16 + 0);
                uint chunkSize = BitConverter.ToUInt32(data, i * 16 + 8);
                uint duration = BitConverter.ToUInt32(data, i * 16 + 12);
                entries.Add(new SuperIndexEntry(offset, chunkSize, duration));
            }

            Entries = entries.ToArray();

            ArrayPool<byte>.Shared.Return(data);

            foreach (var entry in Entries)
            {
                aviFile.FileStream.Seek((long)entry.Offset + 8, SeekOrigin.Begin);
                var index = new OpenDmlStandardIndexChunk(aviFile, null, 0, entry.Size, (long)entry.Offset);
                entry.Index = index;
            }
        }
    }
}
