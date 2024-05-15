using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AviParserLib
{
    public class AviFileIndex
    {
        public static void Parse(string file, out MainAviHeaderChunk mainAviHeader, out AviStreamHeaderChunk aviStreamHeader, out IndexedChunk[] indexedChunks)
        {
            mainAviHeader = null;
            aviStreamHeader = null;
            indexedChunks = null;
            IndexedChunk[] idx1Chunks = null;

            long moviPosition = 0;

            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[12];
                stream.Read(buffer, 0, 12);
                var chunkType = BitConverter.ToUInt32(buffer, 0);
                var size = BitConverter.ToUInt32(buffer, 4);
                var fourCC = BitConverter.ToUInt32(buffer, 8);
                if (chunkType != RiffAtom.RiffListFourCC && fourCC != ListAtom.RiffAviList)
                {
                    return;
                }

                var hdrlFourCC = RiffAtom.MakeFourCC("hdrl");
                var moviFourCC = RiffAtom.MakeFourCC("movi");
                long position = stream.Position;
                while (mainAviHeader == null || aviStreamHeader == null || indexedChunks == null)
                {
                    stream.Read(buffer, 0, 12);
                    chunkType = BitConverter.ToUInt32(buffer, 0);
                    size = BitConverter.ToUInt32(buffer, 4);
                    fourCC = BitConverter.ToUInt32(buffer, 8);
#if DEBUG
                    string chunkTypeStr = RiffAtom.FourCCToString(chunkType);
                    string fourCCStr = RiffAtom.FourCCToString(fourCC);
#endif
                    if (fourCC == hdrlFourCC)
                    {
                        ParseHdrl(stream, position + size, out mainAviHeader, out aviStreamHeader, out indexedChunks);
                    } else if (chunkType == Index1Chunk.FourCC)
                    {
                        stream.Seek(-4, SeekOrigin.Current);
                        idx1Chunks = ParseIdx1(stream, size, moviPosition);
                        if (indexedChunks == null)
                        {
                            indexedChunks = idx1Chunks;
                        }
                        break;
                    } else if (fourCC == moviFourCC)
                    {
                        moviPosition = position + 8;
                    }
                    position = position + 8 + size + size % 2;
                    stream.Seek(position, SeekOrigin.Begin);
                }
            }
        }


        private static void ParseHdrl(FileStream stream, long nextChunkStart, out MainAviHeaderChunk mainAviHeader, out AviStreamHeaderChunk aviStreamHeader, out IndexedChunk[] indexedChunks)
        {
            indexedChunks = null;
            mainAviHeader = null;
            aviStreamHeader = null;

            var buffer = new byte[12];
            var position = stream.Position;

            var strlFourCC = RiffAtom.MakeFourCC("strl");

            while (mainAviHeader == null || aviStreamHeader == null || indexedChunks == null)
            {
                stream.Read(buffer, 0, 12);
                var chunkType = BitConverter.ToUInt32(buffer, 0);
                var size = BitConverter.ToUInt32(buffer, 4);

                if (chunkType == MainAviHeaderChunk.FourCC)
                {
                    stream.Seek(-4, SeekOrigin.Current);
                    mainAviHeader = ParseMainAviHeader(stream);
                } else if (chunkType == RiffAtom.ListFourCC)
                {
                    var fourCC = BitConverter.ToUInt32(buffer, 8);
                    if (fourCC == strlFourCC)
                    {
                        ParseAviStreamHeaderBlock(stream, position + size, out var aviVideoStreamHeader, out var indexedVideoChunks);
                        if (aviVideoStreamHeader != null)
                        {
                            aviStreamHeader = aviVideoStreamHeader;
                        }

                        if (indexedVideoChunks != null)
                        {
                            indexedChunks = indexedVideoChunks;
                        }
                    }
                }
                position = position + 8 + size + size % 2;
                if (position >= nextChunkStart)
                {
                    break;
                }
                stream.Seek(position, SeekOrigin.Begin);
            }
        }
      
        private static MainAviHeaderChunk ParseMainAviHeader(FileStream stream)
        {
            var buffer = new byte[10 * 4];
            stream.Read(buffer, 0, 10 * 4);
            var microSecPerFrame = BitConverter.ToUInt32(buffer, 0 * 4);
            var maxBytePerSec = BitConverter.ToUInt32(buffer, 1 * 4);
            var paddingGranularity = BitConverter.ToUInt32(buffer, 2 * 4);
            var flags = (AviFlag)BitConverter.ToUInt32(buffer, 3 * 4);
            var totalFrames = BitConverter.ToUInt32(buffer, 4 * 4);
            var initialFrames = BitConverter.ToUInt32(buffer, 5 * 4);
            var streams = BitConverter.ToUInt32(buffer, 6 * 4);
            var suggestedBufferSize = BitConverter.ToUInt32(buffer, 7 * 4);
            var width = BitConverter.ToUInt32(buffer, 8 * 4);
            var height = BitConverter.ToUInt32(buffer, 9 * 4);

            return new MainAviHeaderChunk(microSecPerFrame, maxBytePerSec, paddingGranularity, flags, totalFrames, initialFrames, streams, suggestedBufferSize, width, height);
        }

        private static void ParseAviStreamHeaderBlock(FileStream stream, long nextChunkStart, out AviStreamHeaderChunk aviVideoStreamHeader, out IndexedChunk[] indexedVideoChunks)
        {
            aviVideoStreamHeader = null;
            indexedVideoChunks = null;
            IndexedChunk[] indexedChunks = null;

            var buffer = new byte[8];
            var position = stream.Position;

            bool streamHeaderFound = false;
            bool openDmlIndexFound = false;

            while (!streamHeaderFound || !openDmlIndexFound)
            {
                stream.Read(buffer, 0, 8);
                var chunkType = BitConverter.ToUInt32(buffer, 0);
                var size = BitConverter.ToUInt32(buffer, 4);

                if (chunkType == AviStreamHeaderChunk.FourCC)
                {
                    var streamHeader = ParseAviStreamHeader(stream);
                    if (streamHeader.Type == AviStreamHeaderChunk.VideoFourCC)
                    {
                        aviVideoStreamHeader = streamHeader;
                    }
                    streamHeaderFound = true;
                } else if (chunkType == OpenDmlIndexChunkBase.FourCC)
                {
                    indexedChunks = ParseOpenDmlIndex(stream);
                    openDmlIndexFound = true;
                }
                
                position = position + 8 + size + size % 2;
                if (position >= nextChunkStart)
                {
                    break;
                }
                stream.Seek(position, SeekOrigin.Begin);
            }

            if (aviVideoStreamHeader != null)
            {
                indexedVideoChunks = indexedChunks;
            }
        }


        private static AviStreamHeaderChunk ParseAviStreamHeader(FileStream stream)
        {
            var buffer = new byte[15 * 4];
            stream.Read(buffer, 0, 15 * 4);
            var type = BitConverter.ToUInt32(buffer, 0 * 4);
            var handler = BitConverter.ToUInt32(buffer, 1 * 4);
            var flags = (AviStreamFlag)BitConverter.ToUInt32(buffer, 2 * 4);
            var priority = BitConverter.ToUInt16(buffer, 3 * 4);
            var language = BitConverter.ToUInt16(buffer, 3 * 4 + 2);
            var initialFrames = BitConverter.ToUInt32(buffer, 4 * 4);
            var scale = BitConverter.ToUInt32(buffer, 5 * 4);
            var rate = BitConverter.ToUInt32(buffer, 6 * 4);
            var start = BitConverter.ToUInt32(buffer, 7 * 4);
            var length = BitConverter.ToUInt32(buffer, 8 * 4);
            var suggestedBufferSize = BitConverter.ToUInt32(buffer, 9 * 4);
            var quality = BitConverter.ToUInt32(buffer, 10 * 4);
            var sampleSize = BitConverter.ToUInt32(buffer, 11 * 4);
            var left = BitConverter.ToInt16(buffer, 12 * 4);
            var top = BitConverter.ToInt16(buffer, 12 * 4 + 2);
            var right = BitConverter.ToInt16(buffer, 12 * 4 + 4);
            var bottom = BitConverter.ToInt16(buffer, 12 * 4 + 6);

            return new AviStreamHeaderChunk(type, handler, flags, priority, language, initialFrames, scale, rate, start, length, suggestedBufferSize, quality, sampleSize, left, top, right, bottom);
        }


        private static IndexedChunk[] ParseOpenDmlIndex(FileStream stream)
        {
            var indexedChunks = new List<IndexedChunk>();

            var data = ArrayPool<byte>.Shared.Rent(24);
            stream.Read(data, 0, 24);

            var longsPerEntry = BitConverter.ToUInt16(data, 0);
            var indexSubType = data[2];
            var indexType = data[3];
            var entriesInUse = BitConverter.ToUInt32(data, 4);
            var chunkId = BitConverter.ToUInt32(data, 8);

            if (indexType == OpenDmlIndexChunkBase.IndexOfIndexes)
            {
                ArrayPool<byte>.Shared.Return(data);
                data = ArrayPool<byte>.Shared.Rent((int)entriesInUse * 16);
                stream.Read(data, 0, (int)entriesInUse * 16);

                var entries = new SuperIndexEntry[entriesInUse];
                for (int i = 0; i < entriesInUse; i++)
                {
                    ulong offset = BitConverter.ToUInt64(data, i * 16 + 0);
                    uint chunkSize = BitConverter.ToUInt32(data, i * 16 + 8);
                    uint duration = BitConverter.ToUInt32(data, i * 16 + 12);
                    entries[i] = new SuperIndexEntry(offset, chunkSize, duration);
                }
                
                ArrayPool<byte>.Shared.Return(data);

                foreach (var entry in entries)
                {
                    stream.Seek((long)entry.Offset + 8, SeekOrigin.Begin);
                    indexedChunks.AddRange(ParseOpenDmlIndex(stream));
                }
            } else if (indexType == OpenDmlIndexChunkBase.IndexOfChunks)
            {
                var baseOffset = BitConverter.ToUInt64(data, 12);

                ArrayPool<byte>.Shared.Return(data);
                data = ArrayPool<byte>.Shared.Rent((int)entriesInUse * 8);
                stream.Read(data, 0, (int)entriesInUse * 8);

                var entries = new StandardIndexEntry[entriesInUse];
                for (int i = 0; i < entriesInUse; i++)
                {
                    uint offset = BitConverter.ToUInt32(data, i * 8 + 0);
                    uint chunkSize = BitConverter.ToUInt32(data, i * 8 + 4);
                    entries[i] = new StandardIndexEntry(offset, chunkSize);
                }
                
                ArrayPool<byte>.Shared.Return(data);

                foreach (var entry in entries)
                {
                    indexedChunks.Add(new IndexedChunk((long)baseOffset + entry.Offset, entry.RealSize, entry.IsKeyframe));
                }
            }
            else
            {
                ArrayPool<byte>.Shared.Return(data);
            }

            return indexedChunks.ToArray();
        }


        private static IndexedChunk[] ParseIdx1(FileStream stream, uint size, long moviPosition)
        {
            var indexedChunks = new List<IndexedChunk>();

            int nEntries = (int)(size / 16);

            var data = ArrayPool<byte>.Shared.Rent(nEntries * 16);
            stream.Read(data, 0, nEntries * 16);

            var entries = new Index1Entry[nEntries];
            for (int i = 0; i < nEntries; i++)
            {
                uint chunkId = BitConverter.ToUInt32(data, i * 16 + 0);
                IndexFlag flags = (IndexFlag)BitConverter.ToUInt32(data, i * 16 + 4);
                uint chunkOffset = BitConverter.ToUInt32(data, i * 16 + 8);
                uint chunkLength = BitConverter.ToUInt32(data, i * 16 + 12);
                entries[i] = new Index1Entry(chunkId, flags, chunkOffset, chunkLength);
            }

            ArrayPool<byte>.Shared.Return(data);

            foreach (var entry in entries)
            {
                if (((entry.ChunkId & 0xFFFF0000) >> 16) == RiffAtom.VideoTwoCC)
                {
                    indexedChunks.Add(new IndexedChunk(moviPosition + entry.ChunkOffset + 8, entry.ChunkLength, entry.IsKeyframe));
                }
            }
            return indexedChunks.ToArray();
        }

    }
}


