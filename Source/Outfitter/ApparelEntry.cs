// ReSharper disable StyleCop.SA1307
namespace Outfitter
{
    using System.Collections.Generic;

    using RimWorld;

    public struct ApparelEntry
    {
        #region Public Fields

        public Apparel apparel;

        public HashSet<StatDef> equippedOffsets;

        public HashSet<StatDef> infusedOffsets;
        public HashSet<StatDef> statBases;

        #endregion Public Fields
    }
}