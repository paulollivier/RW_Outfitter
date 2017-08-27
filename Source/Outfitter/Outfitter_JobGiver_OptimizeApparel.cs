namespace Outfitter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using RimWorld;

    using UnityEngine;

    using Verse;
    using Verse.AI;

    public static class Outfitter_JobGiver_OptimizeApparel
    {
        #region Public Fields

        public const int ApparelStatCheck = 3750;

        public static List<Apparel> dropped = new List<Apparel>();

        public static Dictionary<Apparel, Pawn> reserved = new Dictionary<Apparel, Pawn>();

        #endregion Public Fields

        #region Private Fields

        private const int ApparelOptimizeCheckIntervalMax = 9000;
        private const int ApparelOptimizeCheckIntervalMin = 6000;
        private const float MinScoreGainToCare = 0.09f;
        private static StringBuilder debugSb;

        #endregion Private Fields

        #region Public Methods

        public static Job GetApparel(this Pawn pawn)
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

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                if (dropped.Contains(wornApparel[i]))
                {
                    continue;
                }

                if (!currentOutfit.filter.Allows(wornApparel[i])
                    && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    dropped.Add(wornApparel[i]);
                    return new Job(JobDefOf.RemoveApparel, wornApparel[i]) { haulDroppedApparel = true };
                }
            }

            Thing thing = null;
            float score = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            foreach (KeyValuePair<Apparel, Pawn> kvp in reserved)
            {
                if (list.Contains(kvp.Key))
                {
                    list.Remove(kvp.Key);
                }
            }

            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return null;
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

                if (!ApparelUtility.HasPartsToWear(pawn, apparel.def))
                {
                    continue;
                }

                float gain = pawn.ApparelScoreGain(apparel);

                if (gain >= MinScoreGainToCare && gain >= score)
                {

                    bool flag = false;

                    foreach (KeyValuePair<Apparel, Pawn> pair in reserved.Where(x => x.Value == pawn))
                    {
                        if (!ApparelUtility.CanWearTogether(pair.Key.def, apparel.def))
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (flag)
                    {
                        continue;
                    }

                    if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
                    {
                        thing = apparel;
                        score = gain;
                    }
                }
            }

            if (thing == null)
            {
                return null;
            }

            reserved.Add((Apparel)thing, pawn);
            return new Job(JobDefOf.Wear, thing);
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
            for (int i = wornApparel.Count - 1; i >= 0; i--)
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