using System;
using System.Text;
using Verse;

namespace Outfitter
{
    using System.Collections.Generic;

    using RimWorld;

    using UnityEngine;

    using Verse.AI;

    [StaticConstructorOnStartup]
    public class Building_BattleSpot : Building
    {
        private int ticksToDespawn;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            Command_Action draft = new Command_Action();
            draft.hotKey = KeyBindingDefOf.CommandColonistDraft;
            draft.defaultLabel = "CommandDraftLabel".Translate();
            draft.defaultDesc = "CommandToggleDraftDesc".Translate();
            draft.icon = TexCommand.Draft;
            draft.activateSound = SoundDefOf.DraftOn;

            //     pris.isActive = (() => this.<> f__this.ForPrisoners);
            draft.action = delegate
                {
                    foreach (Pawn pawn in Find.VisibleMap.mapPawns.FreeColonistsSpawned)
                    {
                        if (pawn.mindState == null)
                            continue;
                        if (pawn.InMentalState)
                            continue;
                        if (pawn.Dead || pawn.Downed)
                            continue;

                        SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(pawn);
                        pawnSave.armorOnly = true;
                        pawnSave.forceStatUpdate = true;
                        Outfitter_JobGiver_GetArmor.GetApparelList(pawn, out List<Thing> apparelList, out List<Thing> toDrop);

                        pawn.jobs.StopAll();

                        if (!toDrop.NullOrEmpty())
                        {
                            for (int i = 0; i < toDrop.Count; i++)
                            {
                                Job jobDrop = new Job(JobDefOf.RemoveApparel, toDrop[i])
                                {
                                    haulDroppedApparel = true

                                };
                                pawn.jobs.jobQueue.EnqueueFirst(jobDrop);
                                jobDrop.locomotionUrgency = LocomotionUrgency.Sprint;
                            }
                        }

                        if (!apparelList.NullOrEmpty())
                        {
                            for (int i = 0; i < apparelList.Count; i++)
                            {
                                if (!pawn.CanReserveAndReach(apparelList[i], PathEndMode.ClosestTouch, Danger.Deadly))
                                    continue;
                                pawn.Reserve(apparelList[i]);
                                Job job = new Job(JobDefOf.Wear, apparelList[i]);
                                job.locomotionUrgency = LocomotionUrgency.Sprint;
                                pawn.jobs.jobQueue.EnqueueLast(job);

                            }
                        }

                        Job jobby = new Job(DefDatabase<JobDef>.GetNamed("GoToDraftOf"))
                        {
                            targetA = this.Position.RandomAdjacentCell8Way(),
                            locomotionUrgency = LocomotionUrgency.Sprint
                        };
                        pawn.jobs.jobQueue.EnqueueLast(jobby);

                    }
                    this.DeSpawn();
                };
            yield return draft;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            this.ticksToDespawn = 3000;
        }

        public override void Tick()
        {
            base.Tick();

            this.ticksToDespawn--;

            if (this.ticksToDespawn == 0)
            {
                this.Destroy(DestroyMode.Deconstruct);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDespawn, "ticksToDespawn");
        }
    }
}
