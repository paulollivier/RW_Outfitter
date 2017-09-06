namespace Outfitter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class JobGiver_OptimizeApparel
    {
        #region Public Fields

        public const int ApparelStatCheck = 3750;


        #endregion Public Fields

        #region Private Fields

        private const int ApparelOptimizeCheckIntervalMax = 9000;
        private const int ApparelOptimizeCheckIntervalMin = 6000;
        private const float MinScoreGainToCare = 0.09f;
        private static StringBuilder debugSb;

        #endregion Private Fields

        #region Public Methods

        public static void DoApparelJobs([NotNull] this Pawn pawn, bool ranged = false)
        {
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(pawn + " tried to run JobGiver_OptimizeApparel without an OutfitTracker", 5643897);
                return;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return;
            }

            List<Thing> allApparel = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            List<Apparel> toWear = new List<Apparel>();

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;

            foreach (Thing t in allApparel.OrderByDescending(x => pawn.ApparelScoreGain((Apparel)x)))
            {
                Apparel apparel = (Apparel)t;

                if (!currentOutfit.filter.Allows(apparel))
                {
                    continue;
                }
                bool skipWorn = false;
                foreach (Apparel wornAp in pawn.apparel.WornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(wornAp.def, apparel.def)
                        && !pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornAp)
                        || pawn.ApparelScoreGain(wornAp) >= pawn.ApparelScoreGain(apparel))
                    {
                        skipWorn = true;
                        break;
                    }
                }
                if (skipWorn)
                {
                    continue;
                }

                if (ranged && apparel.def.thingClass == typeof(ShieldBelt))
                {
                    continue;
                }

                if (apparel.Map.slotGroupManager.SlotGroupAt(apparel.Position) == null)
                {
                    continue;
                }

                if (apparel.IsForbidden(pawn))
                {
                    continue;
                }

                if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, Danger.Deadly, 1))
                {
                    continue;
                }

                bool skip = false;
                foreach (Apparel toWearAp in toWear)
                {
                    if (!ApparelUtility.CanWearTogether(toWearAp.def, apparel.def))
                    {
                        skip = true;
                    }
                }
                if (skip)
                {
                    continue;
                }

                pawn.Reserve(apparel);

                toWear.Add(apparel);
            }

            List<Apparel> toDrop = new List<Apparel>();
            foreach (Apparel wornAp in pawn.apparel.WornApparel)
            {
                bool flag = !currentOutfit.filter.Allows(wornAp)
                            && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornAp)
                            || ranged && wornAp.def.thingClass == typeof(ShieldBelt);
                if (flag)
                {
                    toDrop.Add(wornAp);
                }
            }

            foreach (Apparel ap in toDrop)
            {
                Job job2 = new Job(JobDefOf.RemoveApparel, ap) { haulDroppedApparel = true };
                pawn.jobs.jobQueue.EnqueueLast(job2);
            }

            foreach (Apparel ap in toWear)
            {
                Job job3 = new Job(JobDefOf.Wear, ap);
                pawn.jobs.jobQueue.EnqueueLast(job3);
            }



            SetNextOptimizeTick(pawn);
        }

        // private static NeededWarmth neededWarmth;
        public static bool TryGiveJob_Prefix(ref Job __result, Pawn pawn)
        {
            __result = null;
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(pawn + " tried to run JobGiver_OptimizeApparel without an OutfitTracker", 5643897);
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce("Non-colonist " + pawn + " tried to optimize apparel.", 764323);
                return false;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    return false;
                }
            }
            else
            {
                debugSb = new StringBuilder();
                debugSb.AppendLine(string.Concat("Scanning for ", pawn, " at ", pawn.Position));
            }

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!currentOutfit.filter.Allows(wornApparel[i])
                    && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    __result = new Job(JobDefOf.RemoveApparel, wornApparel[i]) { haulDroppedApparel = true };
                    return false;
                }
            }

            Thing thing = null;
            float num = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            for (int j = 0; j < list.Count; j++)
            {
                Apparel apparel = (Apparel)list[j];
                if (!currentOutfit.filter.Allows(apparel))
                {
                    continue;
                }

                if (apparel.Map.slotGroupManager.SlotGroupAt(apparel.Position) == null)
                {
                    continue;
                }

                if (apparel.IsForbidden(pawn))
                {
                    continue;
                }

                float gain = pawn.ApparelScoreGain(apparel);
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

            if (DebugViewSettings.debugApparelOptimize)
            {
                debugSb.AppendLine("BEST: " + thing);
                Log.Message(debugSb.ToString());
                debugSb = null;
            }

            if (thing == null)
            {
                SetNextOptimizeTick(pawn);
                return false;
            }

            __result = new Job(JobDefOf.Wear, thing);
            return false;
        }

        #endregion Public Methods

        #region Private Methods

        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame
                                                     + Random.Range(
                                                         ApparelOptimizeCheckIntervalMin,
                                                         ApparelOptimizeCheckIntervalMax);
        }

        #endregion Private Methods
    }
}