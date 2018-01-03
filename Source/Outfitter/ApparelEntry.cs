// ReSharper disable StyleCop.SA1307

using System.Collections.Generic;
using RimWorld;

namespace Outfitter
{
    public class ApparelEntry
    {
        public HashSet<StatDef> EquippedOffsets;
        public HashSet<StatDef> InfusedOffsets;
        public HashSet<StatDef> StatBases;

        public ApparelEntry()
        {
            this.EquippedOffsets = new HashSet<StatDef>();
            this.InfusedOffsets  = new HashSet<StatDef>();
            this.StatBases       = new HashSet<StatDef>();
        }
    }
}