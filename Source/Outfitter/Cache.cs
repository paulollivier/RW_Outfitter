using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Outfitter
{
    public static class Cache
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static Dictionary<Apparel, ApparelEntry> ApparelEntries = new Dictionary<Apparel, ApparelEntry>();

        public static Dictionary<Apparel, FloatRange> InsulationDict = new Dictionary<Apparel, FloatRange>();

        public static float GetEquippedStatValue([NotNull] this Apparel apparel, Pawn pawn, StatDef stat)
        {
            float baseStat = pawn.def.statBases.GetStatValueFromList(stat, stat.defaultBaseValue);
            float equippedStatValue = apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            if (!ApparelStatCache.SpecialStats.Contains(stat) && Math.Abs(baseStat) > 0f)
            {
                return ((baseStat + equippedStatValue) / baseStat) - 1;
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

        public static SaveablePawn GetSaveablePawn(this Pawn pawn)
        {
            GameComponent_Outfitter outfitter = Current.Game.GetComponent<GameComponent_Outfitter>();
            foreach (SaveablePawn c in outfitter.PawnCache.Where(c => c.Pawn == pawn))
            {
                return c;
            }

            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            outfitter.PawnCache.Add(n);
            return n;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            // PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

    }
}