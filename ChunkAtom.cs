using System.IO;

namespace AviParserLib
{
    public class ChunkAtom : RiffAtom
    {
        // ReSharper disable once InconsistentNaming
        public ChunkAtom(AviFile aviFile, ListAtom parent, uint fourCC, uint size, long position) : base(aviFile, parent, fourCC, size, position)
        {
        }

        public override string ToString() => $"[{FourCCToString(FourCC)}] ({Size} bytes)";

        public void SeekData(Stream stream)
        {
            stream.Seek(Position + 8, SeekOrigin.Begin);
        }
    }
}
