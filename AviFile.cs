using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;

namespace AviParserLib
{
    public class IndexedChunk
    {
        public long Position { get; }
        public uint Size { get; }
        public bool IsKeyframe { get; }

        public IndexedChunk(long position, uint size, bool isKeyframe)
        {
            Position = position;
            Size = size;
            IsKeyframe = isKeyframe;
        }
    }

    public class AviFile : IDisposable
    {
        internal Stream FileStream { get; }

        internal List<OpenDmlSuperIndexChunk> SuperIndexes { get; } = new List<OpenDmlSuperIndexChunk>();

        internal List<RiffAtom> Atoms { get; } = new List<RiffAtom>();

        internal List<IndexedChunk> IndexedChunks { get; } = new List<IndexedChunk>();

        public AviFile(string file)
        {
            FileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        }

        public void Dispose()
        {
            FileStream.Dispose();
        }

        public ListAtom[] Parse()
        {
            var children = new List<RiffAtom>();
            while (FileStream.Length - FileStream.Position >= 8)
            {
                var atom = RiffAtom.Parse(this, null);
                children.Add(atom);
                if (atom is ListAtom listAtom)
                {
                    listAtom.Parse();
                }
                else
                {
                    Debugger.Break();
                }
                atom.SeekNext();
            }

            return children.Cast<ListAtom>().ToArray();
        }
    }
}
