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


        private static void SetNextOptimizeTick(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + 2 * UnityEngine.Random.Range(ApparelOptimizeCheckIntervalMin, ApparelOptimizeCheckIntervalMax);
        }
    }
}
