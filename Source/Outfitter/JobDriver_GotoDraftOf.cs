// Always needed
// Needed
// Needed
// Needed when you do something with the AI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;

using UnityEngine;

using Verse;
using Verse.AI;
using Verse.Sound;

// Needed when you do something with the Sound

namespace Outfitter
{
    using RimWorld.Planet;

    public class JobDriver_GotoDraftOf : JobDriver
    {
        public JobDriver_GotoDraftOf() { }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);

            Toil arrive = new Toil
                              {
                                  initAction = () =>
                                      {
                                          if (this.CurJob.exitMapOnArrival
                                              && this.pawn.Map.exitMapGrid.IsExitCell(this.pawn.Position))
                                          {
                                              this.TryExitMap();
                                          }
                                      },
                                  defaultCompleteMode = ToilCompleteMode.Instant
                              };
            yield return arrive;

            Toil scatter = new Toil
                               {
                                   initAction = () =>
                                       {
                                           List<Thing> thingsHere =
                                               this.pawn.Map.thingGrid.ThingsListAt(this.pawn.Position);
                                           bool foundOtherPawnHere = false;
                                           for (int i = 0; i < thingsHere.Count; i++)
                                           {
                                               Pawn p = thingsHere[i] as Pawn;
                                               if (p != null && p != this.pawn)
                                               {
                                                   foundOtherPawnHere = true;
                                                   break;
                                               }
                                           }

                                           LocalTargetInfo tp;
                                           if (foundOtherPawnHere)
                                           {
                                               IntVec3 freeCell = CellFinder.RandomClosewalkCellNear(
                                                   this.pawn.Position,
                                                   this.pawn.Map,
                                                   2);
                                               tp = new LocalTargetInfo(freeCell);
                                           }
                                           else tp = new LocalTargetInfo(this.pawn.Position);

                                           this.pawn.pather.StartPath(tp, PathEndMode.OnCell);
                                       },
                                   defaultCompleteMode = ToilCompleteMode.PatherArrival
                               };
            yield return scatter;

            // Set playerController to drafted
            Toil arrivalDraft = new Toil
                                    {
                                        initAction = () => { this.pawn.drafter.Drafted = true; },
                                        defaultCompleteMode = ToilCompleteMode.Instant
                                    };
            yield return arrivalDraft;

        }

        private void TryExitMap()
        {
            if (this.CurJob.failIfCantJoinOrCreateCaravan && !CaravanExitMapUtility.CanExitMapAndJoinOrCreateCaravanNow(this.pawn))
            {
                return;
            }

            this.pawn.ExitMap(true);
        }
    }
}
