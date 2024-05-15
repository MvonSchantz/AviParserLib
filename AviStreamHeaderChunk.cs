using System;
using System.Buffers;

namespace AviParserLib
{
    [Flags]
    public enum AviStreamFlag : uint
    {
        Disabled = 0x00000001,
        VideoPalChanges = 0x00010000,
    }

    public class AviStreamHeaderChunk : ChunkAtom
    {
        public new const uint FourCC = ((uint)(byte)('s') | ((uint)(byte)('t') << 8) | ((uint)(byte)('r') << 16) | ((uint)(byte)('h') << 24));
        
        public static readonly uint VideoFourCC = MakeFourCC("vids");
        public static readonly uint AudioFourCC = MakeFourCC("auds");
        public static readonly uint TextFourCC = MakeFourCC("txts");

        public uint Type { get; }
        public uint Handler { get; }
        public AviStreamFlag Flags { get; }
        public ushort Priority { get; }
        public ushort Language { get; }
        public uint InitialFrames { get; }
        public uint Scale { get; }
        public uint Rate { get; }
        public uint Start { get; }
        public uint Length { get; }
        public uint SuggestedBufferSize { get; }
        public uint Quality { get; }
        public uint SampleSize { get; }
        public short Left { get; }
        public short Top { get; }
        public short Right { get; }
        public short Bottom { get; }

        public bool IsVideo => Type == VideoFourCC;
        public bool IsAudio => Type == AudioFourCC;
        public bool IsText => Type == TextFourCC;

        public Fraction FrameRate { get; }

        public double LengthInSeconds => ((long)Length * (long)Scale) / (double)Rate;

        public TimeSpan StreamLength => TimeSpan.FromSeconds(LengthInSeconds);

        public string Codec => FourCCToString(Handler);

        public AviStreamHeaderChunk(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
            if (fourCC != FourCC)
            {
                throw new ArgumentException();
            }

            SeekData(aviFile.FileStream);

            var buffer = ArrayPool<byte>.Shared.Rent(15 * 4);
            aviFile.FileStream.Read(buffer, 0, 15 * 4);
            Type = BitConverter.ToUInt32(buffer, 0 * 4);
            Handler = BitConverter.ToUInt32(buffer, 1 * 4);
            Flags = (AviStreamFlag)BitConverter.ToUInt32(buffer, 2 * 4);
            Priority = BitConverter.ToUInt16(buffer, 3 * 4);
            Language = BitConverter.ToUInt16(buffer, 3 * 4 + 2);
            InitialFrames = BitConverter.ToUInt32(buffer, 4 * 4);
            Scale = BitConverter.ToUInt32(buffer, 5 * 4);
            Rate = BitConverter.ToUInt32(buffer, 6 * 4);
            Start = BitConverter.ToUInt32(buffer, 7 * 4);
            Length = BitConverter.ToUInt32(buffer, 8 * 4);
            SuggestedBufferSize = BitConverter.ToUInt32(buffer, 9 * 4);
            Quality = BitConverter.ToUInt32(buffer, 10 * 4);
            SampleSize = BitConverter.ToUInt32(buffer, 11 * 4);
            Left = BitConverter.ToInt16(buffer, 12 * 4);
            Top = BitConverter.ToInt16(buffer, 12 * 4 + 2);
            Right = BitConverter.ToInt16(buffer, 12 * 4 + 4);
            Bottom = BitConverter.ToInt16(buffer, 12 * 4 + 6);
            ArrayPool<byte>.Shared.Return(buffer);

            FrameRate = new Fraction(Rate, Scale);
        }

        public AviStreamHeaderChunk(uint type, uint handler, AviStreamFlag flags, ushort priority, ushort language, uint initialFrames, uint scale, uint rate, uint start, uint length, uint suggestedBufferSize, uint quality, uint sampleSize, short left, short top, short right, short bottom) : base(null, null, FourCC, 0, 0)
        {
            Type = type;
            Handler = handler;
            Flags = flags;
            Priority = priority;
            Language = language;
            InitialFrames = initialFrames;
            Scale = scale;
            Rate = rate;
            Start = start;
            Length = length;
            SuggestedBufferSize = suggestedBufferSize;
            Quality = quality;
            SampleSize = sampleSize;
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;

            FrameRate = new Fraction(Rate, Scale);
        }

        public override string ToString() => $"[{FourCCToString(FourCC)}] {FourCCToString(Type)} {Codec} ({Size} bytes)";
    }
}


