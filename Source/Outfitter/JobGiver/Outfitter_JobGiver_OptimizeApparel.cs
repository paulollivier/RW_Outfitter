using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Outfitter
{
    public class Outfitter_JobGiver_OptimizeApparel : ThinkNode_JobGiver
    {
        private const int ApparelOptimizeCheckInterval = 3000;

        private const float MinScoreGainToCare = 0.05f;

        private static StringBuilder debugSb;

        private void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + ApparelOptimizeCheckInterval;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(pawn + " tried to run JobGiver_OptimizeApparel without an OutfitTracker", 5643897);
                return null;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return null;
            }
            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return null;
                }
            }
            else
            {
                debugSb = new StringBuilder();
                debugSb.AppendLine(string.Concat("Scanning for ", pawn, " at ", pawn.Position));
            }
            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                if (!currentOutfit.filter.Allows(wornApparel[i]) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    return new Job(JobDefOf.RemoveApparel, wornApparel[i])
                    {
                        haulDroppedApparel = true
                    };
                }
            }
            Thing thing = null;
            float score = 0f;
            List<Thing> list = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }
            for (int j = 0; j < list.Count; j++)
            {
                Apparel apparel = (Apparel)list[j];
                if (currentOutfit.filter.Allows(apparel))
                {
                    if (Find.SlotGroupManager.SlotGroupAt(apparel.Position) != null)
                    {
                        if (!apparel.IsForbidden(pawn))
                        {
                            float apparelScoreGain = ApparelStatsHelper.ApparelScoreGain(pawn, apparel);
                            if (DebugViewSettings.debugApparelOptimize)
                            {
                                debugSb.AppendLine(apparel.LabelCap + ": " + apparelScoreGain.ToString("F2"));
                            }
                            if (apparelScoreGain >= MinScoreGainToCare && apparelScoreGain >= score)
                            {
                                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                                {
                                    if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                                    {
                                        thing = apparel;
                                        score = apparelScoreGain;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (DebugViewSettings.debugApparelOptimize)
            {
                debugSb.AppendLine("BEST: " + thing);
                Log.Message(debugSb.ToString());
                debugSb = null;
            }
            if (thing == null)
            {
                SetNextOptimizeTick(pawn);
                return null;
            }
            return new Job(JobDefOf.Wear, thing);
        }
        /*
        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            if (ap.def == ThingDefOf.Apparel_PersonalShield && pawn.equipment.Primary != null && !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }
            float num = JobGiver_OptimizeApparel.ApparelScoreRaw(ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, ap.def))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                    {
                        return -1000f;
                    }
                    num -= JobGiver_OptimizeApparel.ApparelScoreRaw(wornApparel[i]);
                    flag = true;
                }
            }
            if (!flag)
            {
                num *= ScoreFactorIfNotReplacing;
            }
            return num;
        }
        */
    }
}
