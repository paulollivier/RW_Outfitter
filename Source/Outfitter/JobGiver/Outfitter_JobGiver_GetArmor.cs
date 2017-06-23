using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Outfitter
{
    using System.Linq;

    public class Outfitter_JobGiver_GetArmor
    {
        private const float MinScoreGainToCare = 0.09f;
        private const int ApparelOptimizeCheckIntervalMin = 6000;

        private const int ApparelOptimizeCheckIntervalMax = 9000;
        private static StringBuilder debugSb;

        public static void GetApparelList(Pawn pawn, out List<Thing> apparelList, out List<Thing> toDrop)
        {
            apparelList = new List<Thing>();
            Dictionary<Thing, float> apparelStats = new Dictionary<Thing, float>();
            toDrop = new List<Thing>();

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


            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int i = wornApparel.Count - 1; i >= 0; i--)
            {
                if (!currentOutfit.filter.Allows(wornApparel[i]) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                {
                    toDrop.Add(wornApparel[i]);
                }
            }
            Thing thing = null;

            float num = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
                SetNextOptimizeTick(pawn);
                return;
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
                            float gain = ApparelStatsHelper.ApparelScoreGain(pawn, apparel, true);
                            if (DebugViewSettings.debugApparelOptimize)
                            {
                                debugSb.AppendLine(apparel.LabelCap + ": " + gain.ToString("F2"));
                            }
                            if (gain >= MinScoreGainToCare)
                            {
                                if (gain >= num)
                                {
                                    if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                                    {
                                        if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, Danger.Deadly, 1))
                                        {
                                            apparelStats.Add(apparel, gain);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            var myList = apparelStats.ToList();

            myList.Sort((x, y) => y.Value.CompareTo(x.Value));

            for (int i = 0; i < myList.Count; i++)
            {
                bool Change = true;
                if (apparelList != null)
                {
                    foreach (Thing ap in apparelList)
                    {
                        if (ap != null && !ApparelUtility.CanWearTogether(ap.def, myList[i].Key.def))
                        {
                            Change = false;
                            break;
                        }
                    }
                }
                if (Change)
                {
                    apparelList.Add(myList[i].Key);
                }


            }

            if (thing == null)
            {
                SetNextOptimizeTick(pawn);
            }
        }

        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 2 * UnityEngine.Random.Range(ApparelOptimizeCheckIntervalMin, ApparelOptimizeCheckIntervalMax);
        }
    }
}
