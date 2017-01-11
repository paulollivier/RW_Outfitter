using System;
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
        private const float MinScoreGainToCare = 0.09f;
        private const int ApparelOptimizeCheckIntervalMin = 6000;

        private const int ApparelOptimizeCheckIntervalMax = 9000;
        private static StringBuilder debugSb;

        [Detour(typeof(JobGiver_OptimizeApparel), bindingFlags = (BindingFlags.Instance | BindingFlags.NonPublic))]
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
                debugSb.AppendLine(string.Concat(new object[]
                {
            "Scanning for ",
            pawn,
            " at ",
            pawn.Position
                }));
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
            float num = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
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
                    if (apparel.Map.slotGroupManager.SlotGroupAt(apparel.Position) != null)
                    {
                        if (!apparel.IsForbidden(pawn))
                        {
                            float gain = ApparelStatsHelper.ApparelScoreGain(pawn, apparel);
                            if (DebugViewSettings.debugApparelOptimize)
                            {
                                debugSb.AppendLine(apparel.LabelCap + ": " + gain.ToString("F2"));
                            }
                            if (gain >= MinScoreGainToCare && gain >= num)
                            {
                                if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                                {
                                    if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                                    {
                                        thing = apparel;
                                        num = gain;
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
                this.SetNextOptimizeTick(pawn);
                return null;
            }
            return new Job(JobDefOf.Wear, thing);
        }

        private void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + UnityEngine.Random.Range(ApparelOptimizeCheckIntervalMin, ApparelOptimizeCheckIntervalMax);
        }
    }
}
