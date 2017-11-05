namespace Outfitter
{
    using RimWorld;

    using Verse;

    public class SpecialThingFilterWorker_ApparelWithoutHitpoints : SpecialThingFilterWorker
    {
        public override bool CanEverMatch(ThingDef def)
        {
            return def.IsApparel && !def.useHitPoints;
        }

        public override bool Matches(Thing t)
        {
            return t.def.IsApparel && !t.def.useHitPoints;
        }
    }
}