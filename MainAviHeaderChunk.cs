using System;
using System.Buffers;

namespace AviParserLib
{
    [Flags]
    public enum AviFlag : uint
    {
        // ReSharper disable InconsistentNaming
        HasIndex = 0x00000010,
        MustUseIndex = 0x00000020,
        IsInterleaved = 0x00000100,
        TrustCkType = 0x00000800,
        WasCaptureFile = 0x00010000,
        Copyrighted = 0x00020000,
        // ReSharper restore InconsistentNaming
    }

    public class MainAviHeaderChunk : ChunkAtom
    {
        // ReSharper disable once InconsistentNaming
        public new const uint FourCC = ((uint)(byte)('a') | ((uint)(byte)('v') << 8) | ((uint)(byte)('i') << 16) | ((uint)(byte)('h') << 24));

        public uint MicroSecPerFrame { get; }
        public uint MaxBytePerSec { get; }
        public uint PaddingGranularity { get; }
        public AviFlag Flags { get; }
        public uint TotalFrames { get; }
        public uint InitialFrames { get; }
        public uint Streams { get; }
        public uint SuggestedBufferSize { get; }
        public uint Width { get; }
        public uint Height { get; }

        // ReSharper disable once InconsistentNaming
        public MainAviHeaderChunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            if (fourCC != FourCC)
            {
                throw new ArgumentException();
            }

            SeekData(aviFile.FileStream);

            var buffer = ArrayPool<byte>.Shared.Rent(10 * 4);
            aviFile.FileStream.Read(buffer, 0, 10 * 4);
            MicroSecPerFrame = BitConverter.ToUInt32(buffer, 0 * 4);
            MaxBytePerSec = BitConverter.ToUInt32(buffer, 1 * 4);
            PaddingGranularity = BitConverter.ToUInt32(buffer, 2 * 4);
            Flags = (AviFlag)BitConverter.ToUInt32(buffer, 3 * 4);
            TotalFrames = BitConverter.ToUInt32(buffer, 4 * 4);
            InitialFrames = BitConverter.ToUInt32(buffer, 5 * 4);
            Streams = BitConverter.ToUInt32(buffer, 6 * 4);
            SuggestedBufferSize = BitConverter.ToUInt32(buffer, 7 * 4);
            Width = BitConverter.ToUInt32(buffer, 8 * 4);
            Height = BitConverter.ToUInt32(buffer, 9 * 4);
            ArrayPool<byte>.Shared.Return(buffer);
        }

        public MainAviHeaderChunk(uint microSecPerFrame, uint maxBytePerSec, uint paddingGranularity, AviFlag flags, uint totalFrames, uint initialFrames, uint streams, uint suggestedBufferSize, uint width, uint height) : base(null, null, FourCC, 0, 0)
        {
            MicroSecPerFrame = microSecPerFrame;
            MaxBytePerSec = maxBytePerSec;
            PaddingGranularity = paddingGranularity;
            Flags = flags;
            TotalFrames = totalFrames;
            InitialFrames = initialFrames;
            Streams = streams;
            SuggestedBufferSize = suggestedBufferSize;
            Width = width;
            Height = height;
        }
    }
}
