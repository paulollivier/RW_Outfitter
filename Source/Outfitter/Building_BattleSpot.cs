using System;
using System.Text;
using Verse;

namespace Outfitter
{
    using System.Collections.Generic;
    using System.Linq;

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

            // pris.isActive = (() => this.<> f__this.ForPrisoners);
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
                    //    GetApparelList(pawn, out List<Thing> apparelList, out List<Thing> toDrop);
                        GetApparelList(pawn, out List<Thing> apparelList);

                        pawn.jobs.StopAll();

                        if (!apparelList.NullOrEmpty())
                        {
                            for (int i = 0; i < apparelList.Count; i++)
                            {
                                if (!pawn.CanReserveAndReach(apparelList[i], PathEndMode.ClosestTouch, Danger.Some))
                                    continue;
                                pawn.Reserve(apparelList[i]);
                                Job job =
                                    new Job(JobDefOf.Wear, apparelList[i])
                                        {
                                            locomotionUrgency = LocomotionUrgency
                                                .Sprint
                                        };
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
        private const float MinScoreGainToCare = 0.09f;

        public static void GetApparelList(Pawn pawn, out List<Thing> apparelList)
        {
            apparelList = new List<Thing>();
            Dictionary<Thing, float> apparelStats = new Dictionary<Thing, float>();

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

            Thing thing = null;

            float num = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
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
                            float gain = pawn.ApparelScoreGain(apparel);

                            if (gain >= MinScoreGainToCare)
                            {
                                if (gain >= num)
                                {
                                    if (ApparelUtility.HasPartsToWear(pawn, apparel.def))
                                    {
                                        if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger(), 1))
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

            List<KeyValuePair<Thing, float>> myList = apparelStats.ToList();

            myList.Sort((x, y) => y.Value.CompareTo(x.Value));

            // Iterate through the list to only add the highest score to this list.
            for (int i = 0; i < myList.Count; i++)
            {
                bool change = true;
                if (!apparelList.NullOrEmpty())
                {
                    foreach (Thing ap in apparelList)
                    {
                        if (!ApparelUtility.CanWearTogether(ap.def, myList[i].Key.def))
                        {
                            change = false;
                            break;
                        }
                    }
                }

                if (change)
                {
                    apparelList.Add(myList[i].Key);
                }
            }
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
