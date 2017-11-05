namespace Outfitter
{
    using RimWorld;

    using Verse;

    public class SpecialThingFilterWorker_DeadmansApparel : SpecialThingFilterWorker
    {
        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsApparel && def.apparel.careIfWornByCorpse;
        }

        public override bool Matches(Thing t)
        {
            Apparel apparel = t as Apparel;
            return apparel != null && apparel.WornByCorpse;
        }
    }
}