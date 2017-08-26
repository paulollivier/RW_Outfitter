// ReSharper disable StyleCop.SA1307
namespace Outfitter
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using RimWorld;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct ApparelEntry
    {
        public Apparel apparel;

        public HashSet<StatDef> equippedOffsets;

        public HashSet<StatDef> statBases;

        public HashSet<StatDef> infusedOffsets;
    }
}