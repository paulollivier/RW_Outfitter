namespace Outfitter
{
    using System.Collections.Generic;

    using RimWorld;

    public struct ApparelEntry
    {
        public Apparel apparel;

        public HashSet<StatDef> equippedOffsets;

        public HashSet<StatDef> statBases;

        public HashSet<StatDef> infusedOffsets;
    }
}