using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;

namespace AviParserLib
{
    public abstract class RiffAtom
    {
        public static readonly ushort VideoTwoCC = MakeTwoCC("dc");
        public static readonly ushort AudioTwoCC = MakeTwoCC("wb");
        public static readonly ushort TextTwoCC = MakeTwoCC("tx");

        public static readonly uint RiffListFourCC = MakeFourCC("RIFF");
        public static readonly uint ListFourCC = MakeFourCC("LIST");

        public static readonly ushort Index = MakeTwoCC("ix");

        // ReSharper disable once InconsistentNaming
        public uint FourCC { get; protected set; }

        public string FourCCStr => FourCCToString(FourCC);


        // ReSharper disable once InconsistentNaming
        public ushort FourCCHigh => (ushort)((FourCC & 0xFFFF0000) >> 16);
        // ReSharper disable once InconsistentNaming
        public ushort FourCCLow => (ushort)(FourCC & 0x0000FFFF);

        public string FourCCHighStr => TwoCCToString(FourCCHigh);
        public string FourCCLowStr => TwoCCToString(FourCCLow);

        public AviFile File { get; }
        public ListAtom Parent { get; protected set; }

        public uint Size { get; protected set; }

        public long Position { get; protected set; }

        // ReSharper disable once InconsistentNaming
        protected RiffAtom(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position)
        {
            File = aviFile;
            Parent = parent;
            FourCC = fourCC;
            Size = size;
            Position = position;
        }

        // ReSharper disable once InconsistentNaming
        public static uint MakeFourCC(int ch0, int ch1, int ch2, int ch3)
        {
            return ((uint)(byte)(ch0) | ((uint)(byte)(ch1) << 8) | ((uint)(byte)(ch2) << 16) | ((uint)(byte)(ch3) << 24));
        }

        // ReSharper disable once InconsistentNaming
        public static uint MakeFourCC(string fcc)
        {
            return MakeFourCC(fcc[0], fcc[1], fcc[2], fcc[3]);
        }

        public static string FourCCToString(uint fourCC)
        {
            var fourCCcString = new string
            (
                new[]
                {
                    (char)(fourCC & 0xFF),
                    (char)((fourCC & 0xFF00) >> 8),
                    (char)((fourCC & 0xFF0000) >> 16),
                    (char)((fourCC & 0xFF000000U) >> 24)
                }
            );
            return fourCCcString;
        }

        // ReSharper disable once InconsistentNaming
        public static ushort MakeTwoCC(int ch0, int ch1)
        {
            return (ushort)((ushort)(byte)(ch0) | ((ushort)(byte)(ch1) << 8));
        }

        // ReSharper disable once InconsistentNaming
        public static ushort MakeTwoCC(string fcc)
        {
            return MakeTwoCC(fcc[0], fcc[1]);
        }

        public static string TwoCCToString(ushort twoCC)
        {
            var twoCCString = new string
            (
                new[]
                {
                    (char)(twoCC & 0xFF),
                    (char)((twoCC & 0xFF00) >> 8),
                }
            );
            return twoCCString;
        }

        public void SeekNext()
        {
            uint pad = Size % 2;
            File.FileStream.Seek(Position + 8 + Size + pad, SeekOrigin.Begin);
        }

        
        public static RiffAtom Parse(AviFile aviFile, ListAtom parent)
        {
            byte indexSubType;
            byte indexType;

            long position = aviFile.FileStream.Position;
            var buffer = ArrayPool<byte>.Shared.Rent(8);
            aviFile.FileStream.Read(buffer, 0, 8);
            var chunkType = BitConverter.ToUInt32(buffer, 0);
            var size = BitConverter.ToUInt32(buffer, 4);

            if (chunkType == RiffListFourCC || chunkType == ListFourCC)
            {
                aviFile.FileStream.Read(buffer, 0, 4);
                var fourCC = BitConverter.ToUInt32(buffer, 0);
                ArrayPool<byte>.Shared.Return(buffer);
                var list = new ListAtom(aviFile, parent, chunkType, size, fourCC, position);
                aviFile.Atoms.Add(list);
                return list;
            }
            else
            {
                ArrayPool<byte>.Shared.Return(buffer);
                switch (chunkType)
                {
                    case MainAviHeaderChunk.FourCC:
                        var aviHeaderChunk = new MainAviHeaderChunk(aviFile, parent, chunkType, size, position);
                        aviFile.Atoms.Add(aviHeaderChunk);
                        return aviHeaderChunk;
                    case AviStreamHeaderChunk.FourCC:
                        var streamHeaderChunk = new AviStreamHeaderChunk(aviFile, parent, chunkType, size, position);
                        aviFile.Atoms.Add(streamHeaderChunk);
                        return streamHeaderChunk;
                    case AviExtHeaderChunk.FourCC:
                        var extHeaderChunk = new AviExtHeaderChunk(aviFile, parent, chunkType, size, position);
                        aviFile.Atoms.Add(extHeaderChunk);
                        return extHeaderChunk;
                    case Index1Chunk.FourCC:
                        var index1Chunk = new Index1Chunk(aviFile, parent, chunkType, size, position);
                        aviFile.Atoms.Add(index1Chunk);
                        return index1Chunk;
                    case OpenDmlIndexChunkBase.FourCC:
                        aviFile.FileStream.Seek(position + 12, SeekOrigin.Begin);
                        indexSubType = (byte)aviFile.FileStream.ReadByte();
                        indexType = (byte)aviFile.FileStream.ReadByte();
                        switch (indexType)
                        {
                            case OpenDmlIndexChunkBase.IndexOfIndexes:
                                var superIndex = new OpenDmlSuperIndexChunk(aviFile, parent, chunkType, size, position);
                                aviFile.SuperIndexes.Add(superIndex);
                                aviFile.Atoms.Add(superIndex);
                                return superIndex;
                            case OpenDmlIndexChunkBase.IndexOfChunks:
                                switch (indexSubType)
                                {
                                    case 0:
                                        var standardIndex = new OpenDmlStandardIndexChunk(aviFile, parent, chunkType, size, position);
                                        aviFile.Atoms.Add(standardIndex);
                                        return standardIndex;
                                    case OpenDmlIndexChunkBase.Index2Field:
                                    default:
                                        Debugger.Break();
                                        throw new NotImplementedException();
                                }
                            default:
                                Debugger.Break();
                                throw new NotImplementedException();
                        }
                    default:
                        var atom2 = new ChunkAtom(aviFile, parent, chunkType, size, position);
                        aviFile.Atoms.Add(atom2);
                        return atom2;
                }
            }
        }
    }
}

