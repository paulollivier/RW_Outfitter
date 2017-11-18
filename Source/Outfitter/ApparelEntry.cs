// ReSharper disable StyleCop.SA1307

namespace Outfitter
{
    using System.Collections.Generic;

    using RimWorld;

    public class ApparelEntry
    {
        public HashSet<StatDef> equippedOffsets;

        public HashSet<StatDef> infusedOffsets;

        public HashSet<StatDef> statBases;

        public ApparelEntry()
        {
            this.equippedOffsets = new HashSet<StatDef>();
            this.infusedOffsets = new HashSet<StatDef>();
            this.statBases = new HashSet<StatDef>();
        }
    }
}