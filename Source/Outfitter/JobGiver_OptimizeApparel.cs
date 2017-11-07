namespace Outfitter
{
    using JetBrains.Annotations;
    using RimWorld;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;
    using Verse;
    using Verse.AI;

    public static class JobGiver_OptimizeApparel
    {
        public const int ApparelStatCheck = 3750;

        private const int ApparelOptimizeCheckIntervalMax = 9000;

        private const int ApparelOptimizeCheckIntervalMin = 6000;

        private const float MinScoreGainToCare = 0.09f;

        private static StringBuilder debugSb;

        private static Apparel lastItem;

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
                    if (!ApparelUtility.CanWearTogether(wornAp.def, apparel.def, pawn.RaceProps.body)
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

                if (toWear.Any(x => !ApparelUtility.CanWearTogether(x.def, apparel.def, pawn.RaceProps.body)))
                {
                    continue;
                }

                Job job3 = new Job(JobDefOf.Wear, apparel);
                pawn.Reserve(apparel, job3);
                pawn.jobs.jobQueue.EnqueueLast(job3);

                toWear.Add(apparel);
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

            foreach (Apparel ap in wornApparel)
            {
                if (!currentOutfit.filter.Allows(ap)
                    && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap))
                {
                    __result = new Job(JobDefOf.RemoveApparel, ap) { haulDroppedApparel = true };
                    return false;
                }
                ApparelStatCache conf =pawn.GetApparelStatCache();

                if (conf.ApparelScoreRaw(ap, pawn) < 0f
                    && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(ap))
                {
                    __result = new Job(JobDefOf.RemoveApparel, ap) { haulDroppedApparel = true };
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

            foreach (Thing t in list)
            {
                Apparel apparel = (Apparel)t;
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

                // this blocks pawns constantly switching between the recent apparel, due to shifting calculations
                // not very elegant but working
               // if (pawn.GetApparelStatCache().recentApparel.Contains(apparel))
               // {
               //     gain *= 0.01f;
               // }

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

           // foreach (Apparel apparel in wornApparel)
           // {
           //     pawn.GetApparelStatCache().recentApparel.Add(apparel);
           // }

            __result = new Job(JobDefOf.Wear, thing);
            return false;
        }

        private static void SetNextOptimizeTick([NotNull] Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame
                                                     + Random.Range(
                                                         ApparelOptimizeCheckIntervalMin,
                                                         ApparelOptimizeCheckIntervalMax);
           // pawn.GetApparelStatCache().recentApparel.Clear();

        }
    }
}