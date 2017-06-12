using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine; // Always needed
using RimWorld; // Needed
using Verse; // Needed
using Verse.AI; // Needed when you do something with the AI
using Verse.Sound; // Needed when you do something with the Sound

namespace Outfitter
{
    using RimWorld.Planet;

    public class JobDriver_GotoDraftOf : JobDriver
    {
        public JobDriver_GotoDraftOf() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil arrive = new Toil();
            arrive.initAction = () =>
                {
                    if (CurJob.exitMapOnArrival && pawn.Map.exitMapGrid.IsExitCell(this.pawn.Position))
                    {
                        this.TryExitMap();
                    }
                };
            arrive.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrive;

            Toil scatter = new Toil();
            scatter.initAction = () =>
                {
                    List<Thing> thingsHere = this.pawn.Map.thingGrid.ThingsListAt(pawn.Position);
                    bool foundOtherPawnHere = false;
                    for (int i = 0; i < thingsHere.Count; i++)
                    {
                        Pawn p = thingsHere[i] as Pawn;
                        if (p != null && p != pawn)
                        {
                            foundOtherPawnHere = true;
                            break;
                        }
                    }

                    LocalTargetInfo tp;
                    if (foundOtherPawnHere)
                    {
                        IntVec3 freeCell = CellFinder.RandomClosewalkCellNear(pawn.Position, this.pawn.Map, 2);
                        tp = new LocalTargetInfo(freeCell);
                    }
                    else
                        tp = new LocalTargetInfo(pawn.Position);

                    pawn.pather.StartPath(tp, PathEndMode.OnCell);
                };
            scatter.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            yield return scatter;

            // Set playerController to drafted
            Toil arrivalDraft = new Toil();
            arrivalDraft.initAction = () =>
                {
                    pawn.drafter.Drafted = true;
                };
            arrivalDraft.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return arrivalDraft;

        }
        private void TryExitMap()
        {
            if (base.CurJob.failIfCantJoinOrCreateCaravan && !CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(this.pawn))
            {
                return;
            }
            this.pawn.ExitMap(true);
        }
    }
}
