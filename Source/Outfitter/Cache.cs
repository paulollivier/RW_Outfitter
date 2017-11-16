namespace Outfitter
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using RimWorld;

    using Verse;

    public static class Cache
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static Dictionary<Apparel, ApparelEntry> ApparelEntries = new Dictionary<Apparel, ApparelEntry>();

        public static float GetEquippedStatValue([NotNull] this Apparel apparel, StatDef stat)
        {
            float currentStat = apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            // {
            //     float baseStat = apparel.GetStatValue(stat, true);
            //     float currentStat = baseStat;
            //     currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.);
            // 
           // DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Cold, out float infInsulationCold);
           // DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Heat, out float infInsulationHeat);
            // 
            //     if (baseStat == 0)
            //         return currentStat;
            //     else
            //         return currentStat / baseStat;
            // }

            // float pawnStat = p.GetStatValue(stat);
            //
            // var x = pawnStat;
            // if (!p.apparel.WornApparel.Contains(apparel))
            // {
            //     x += currentStat;
            // }
            // currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            // if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            // {
            // return apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            // }

            return currentStat;
        }
    }
}