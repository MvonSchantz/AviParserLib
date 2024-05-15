using System;
using System.Collections.Generic;
using System.IO;

namespace AviParserLib
{
    public class ListAtom : RiffAtom
    {
        public static readonly uint RiffAviList = MakeFourCC("AVI ");
        public static readonly uint RiffAvixList = MakeFourCC("AVIX");

        public uint List { get; }

        public RiffAtom[] Children { get; private set; }

        // ReSharper disable once InconsistentNaming
        public ListAtom(AviFile aviFile, ListAtom parent, uint list, uint size, uint fourCC, long position) : base(aviFile, parent, fourCC, size, position)
        {
            List = list;
        }

        public override string ToString() => $"{FourCCToString(List)} [{FourCCToString(FourCC)}] ({Size} bytes)";

        public void SeekData()
        {
            File.FileStream.Seek(Position + 12, SeekOrigin.Begin);
        }

        public uint DataLeft()
        {
            long nextAtom = Position + 8 + Size;
            long left = nextAtom - File.FileStream.Position;
            if (left < 0)
            {
                return 0;
            }
            return (uint)left;
        }

        public void Parse()
        {
            if (Size - 4 < 8)
            {
                Children = Array.Empty<RiffAtom>();
                return;
            }
            SeekData();
            var children = new List<RiffAtom>();
            while (DataLeft() >= 8)
            {
                var atom = RiffAtom.Parse(File, this);
                children.Add(atom);
                if (atom is ListAtom listAtom)
                {
                    listAtom.Parse();
                }
                atom.SeekNext();
            }
            Children = children.ToArray();
        }
    }
}
