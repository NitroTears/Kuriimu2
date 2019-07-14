﻿using Kompression.LempelZiv.Decoders;
using Kompression.LempelZiv.Encoders;
using Kompression.LempelZiv.MatchFinder;
using Kompression.LempelZiv.Parser;

/* Used in Super Robot Taizen Z and MTV archive */
// TODO: Find out that PS2 game from IcySon55

namespace Kompression.LempelZiv
{
    public class LzssVlc : BaseLz
    {
        protected override ILzMatchFinder CreateMatchFinder(int inputLength)
        {
            return new SuffixTreeMatchFinder(4, inputLength);
        }

        protected override ILzEncoder CreateEncoder()
        {
            return new LzssVlcEncoder();
        }

        protected override ILzParser CreateParser(ILzMatchFinder finder, ILzEncoder encoder)
        {
            return new OptimalParser(finder, encoder);
        }

        protected override ILzDecoder CreateDecoder()
        {
            return new LzssVlcDecoder();
        }
    }
}