using System.Collections.Generic;
using Infused;
using Outfitter;
using RimWorld;

using Verse;

namespace OutfitterInfused
{
    using System.Linq;

    using Def = Infused.Def;

    public class GameComponent_OutfitterInfused : GameComponent
    {
        public GameComponent_OutfitterInfused()
        {
            Log.Message("Outfitter with Infused Initialized");
            ApparelStatCache.ApparelScoreRaw_PawnStatsHandlers += ApparelScoreRaw_PawnStatsHandlers;
            ApparelStatCache.ApparelScoreRaw_FillInfusedStat += ApparelScoreRaw_FillInfusedStat;
            ApparelStatCache.Ignored_WTHandlers += Ignored_WTHandlers;
        }

        public GameComponent_OutfitterInfused(Game game)
        {
        }

        public static void ApparelScoreRaw_FillInfusedStat(Apparel apparel, StatDef parentStat, ref HashSet<StatDef> infusedOffsets)
        {
            if (apparel.TryGetInfusions(out InfusionSet inf))
            {
                StatMod mod;

                Def prefix = inf.prefix;
                Def suffix = inf.suffix;

                if (prefix != null && prefix.TryGetStatValue(parentStat, out mod))
                {
                    infusedOffsets.Add(parentStat);
                }

                if (suffix != null && suffix.TryGetStatValue(parentStat, out mod))
                {
                    infusedOffsets.Add(parentStat);
                }
            }
        }

        public static void ApparelScoreRaw_PawnStatsHandlers(Apparel apparel, StatDef statPriority, ref float val)
        {
            if (apparel.TryGetInfusions(out InfusionSet inf))
            {
                Def prefix = inf.prefix;
                Def suffix = inf.suffix;

                float statInfusedPrefix = 0f;
                float statInfusedSuffix = 0f;

                if (prefix != null && prefix.TryGetStatValue(statPriority, out StatMod mod))
                {
                    statInfusedPrefix += mod.offset;
                    statInfusedPrefix += mod.multiplier - 1;
                }

                if (suffix != null && suffix.TryGetStatValue(statPriority, out mod))
                {
                    statInfusedSuffix += mod.offset;
                    statInfusedSuffix += mod.multiplier - 1;
                }

                val += statInfusedPrefix + statInfusedSuffix;
            }
        }

        public static void Ignored_WTHandlers(ref List<StatDef> allApparelStats)
        {
            // add all stat modifiers from all infusions
            foreach (KeyValuePair<StatDef, StatMod> mod in DefDatabase<Def>.AllDefsListForReading.SelectMany(
                infusion => infusion.stats))
            {
                if (!allApparelStats.Contains(mod.Key))
                {
                    allApparelStats.Add(mod.Key);
                }
            }
        }
    }
}