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

        public static float GetEquippedStatValue([NotNull] this Apparel apparel, Pawn pawn, StatDef stat)
        {
            float baseStat = pawn.def.statBases.GetStatValueFromList(stat, stat.defaultBaseValue);
            float equippedStatValue = apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            // float currentStat = apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);


            //  ApparelStatCache.DoApparelScoreRaw_PawnStatsHandlers(apparel, stat, out float statFloat);
            //  equippedStatValue += statFloat;

            //  if (pawn == apparel.Wearer)
            //  {
            //      baseStat -= equippedStatValue;
            //  }

           // if (pawn.story != null)
           // {
           //     for (int k = 0; k < pawn.story.traits.allTraits.Count; k++)
           //     {
           //         baseStat += pawn.story.traits.allTraits[k].OffsetOfStat(stat);
           //         baseStat *= pawn.story.traits.allTraits[k].MultiplierOfStat(stat);
           //     }
           // }

            if (baseStat != 0)
            {
                return ((baseStat + equippedStatValue) / baseStat) -1;
            }
            return equippedStatValue;

            // float pawnStat = p.GetStatValue(stat);
            // var x = pawnStat;
            // if (!p.apparel.WornApparel.Contains(apparel))
            // {
            // x += currentStat;
            // }
            // currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            // if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            // {
            // return apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            // }
            return equippedStatValue;
        }
    }
}