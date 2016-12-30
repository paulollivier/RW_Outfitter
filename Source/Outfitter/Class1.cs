using RimWorld;
using Verse;

namespace Outfitter
{
    public class SpecialThingFilterWorker_DeadmansApparel : SpecialThingFilterWorker
    {
        public override bool Matches(Thing t)
        {
            Apparel apparel = t as Apparel;
            return apparel != null && apparel.WornByCorpse;
        }

        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsApparel && def.apparel.careIfWornByCorpse;
        }
    }
}

