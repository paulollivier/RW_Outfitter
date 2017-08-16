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

                        if (!this.AlreadySatisfiedWithCurrentWeapon(pawn) && !pawn.story.WorkTagIsDisabled(WorkTags.Violent)
                            && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            Thing thing = GenClosest.ClosestThingReachable(
                                apparelList.NullOrEmpty() ? pawn.Position : apparelList.Last().Position,
                                pawn.Map,
                                ThingRequest.ForGroup(ThingRequestGroup.Weapon),
                                PathEndMode.OnCell,
                                TraverseParms.For(pawn),
                                20f,
                                x => pawn.CanReserve(x) && this.ShouldEquip(x, pawn),
                                null,
                                0,
                                15);
                            if (thing != null)
                            {
                                pawn.Reserve(thing);
                                pawn.jobs.jobQueue.EnqueueLast(new Job(JobDefOf.Equip, thing));
                            }
                        }


                        Job jobby = new Job(DefDatabase<JobDef>.GetNamed("GoToDraftOf"))
                        {
                            targetA = this.Position.RandomAdjacentCell8Way(),
                            locomotionUrgency = LocomotionUrgency.Sprint
                        };
                        jobby.playerForced = true;
                        pawn.jobs.jobQueue.EnqueueLast(jobby);


                    }

                    this.DeSpawn();
                };
            yield return draft;
        }

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private bool ShouldEquip(Thing newWep, Pawn pawn)
        {
            return this.GetWeaponScore(pawn, newWep) > this.GetWeaponScore(pawn, pawn.equipment.Primary);
        }

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private float GetWeaponScore(Pawn pawn, Thing wep)
        {
            if (wep == null)
            {
                return 0;
            }
            if (wep.def.IsMeleeWeapon && wep.GetStatValue(StatDefOf.MeleeWeapon_DamageAmount) < this.MinMeleeWeaponDamageThreshold)
            {
                return 0;
            }
            //if (this.preferBuildingDestroyers && wep.TryGetComp<CompEquippable>().PrimaryVerb.verbProps.ai_IsBuildingDestroyer)
            //{
            //    return 3;
            //}

            if (wep.TryGetComp<CompExplosive>() != null)
            {
                return -1;
            }

            if (wep.def.IsRangedWeapon)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                {
                    return -1;
                }
                return 2 * pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
            }
            return 1 * pawn.skills.GetSkill(SkillDefOf.Melee).Level;
        }

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private float MinMeleeWeaponDamageThreshold
        {
            get
            {
                List<VerbProperties> verbs = ThingDefOf.Human.Verbs;
                float num = 0f;
                for (int i = 0; i < verbs.Count; i++)
                {
                    if (verbs[i].linkedBodyPartsGroup == BodyPartGroupDefOf.LeftHand || verbs[i].linkedBodyPartsGroup == BodyPartGroupDefOf.RightHand)
                    {
                        num = (float)verbs[i].meleeDamageBaseAmount;
                        break;
                    }
                }
                return num + 3f;
            }
        }


        private const float MinScoreGainToCare = 0.09f;

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private bool AlreadySatisfiedWithCurrentWeapon(Pawn pawn)
        {
            ThingWithComps primary = pawn.equipment.Primary;
            if (primary == null)
            {
                return false;
            }
            //if (this.preferBuildingDestroyers)
            //{
            //    if (!pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.ai_IsBuildingDestroyer)
            //    {
            //        return false;
            //    }
            //}
            // else 


            return true;
        }


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
                                        if (pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger()))
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
