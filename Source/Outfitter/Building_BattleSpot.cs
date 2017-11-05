namespace Outfitter
{
    using JetBrains.Annotations;
    using RimWorld;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Verse;
    using Verse.AI;

    [StaticConstructorOnStartup]

    // ReSharper disable once UnusedMember.Global
    public class Building_BattleSpot : Building
    {
        private const float MinScoreGainToCare = 0.09f;

        private int ticksToDespawn;

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private float MinMeleeWeaponDamageThreshold
        {
            get
            {
                List<VerbProperties> verbs = ThingDefOf.Human.Verbs;
                float num = 0f;
                for (int i = 0; i < verbs.Count; i++)
                {
                    if (verbs[i].linkedBodyPartsGroup == BodyPartGroupDefOf.LeftHand
                        || verbs[i].linkedBodyPartsGroup == BodyPartGroupDefOf.RightHand)
                    {
                        num = verbs[i].meleeDamageBaseAmount;
                        break;
                    }
                }

                return num + 3f;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDespawn, "ticksToDespawn");
        }

        // ReSharper disable once MethodTooLong
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo c in base.GetGizmos())
            {
                yield return c;
            }

            Command_Action draft = new Command_Action
                                       {
                                           hotKey = KeyBindingDefOf.CommandColonistDraft,
                                           defaultLabel = "CommandDraftLabel".Translate(),
                                           defaultDesc = "CommandToggleDraftDesc".Translate(),
                                           icon = TexCommand.Draft,
                                           activateSound = SoundDefOf.DraftOn
                                       };

            List<Thing> weaponList = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
                .Where(x => !x.Map.reservationManager.IsReservedByAnyoneOf(x, Faction.OfPlayer) && x.def.IsRangedWeapon)
                .OrderBy(x => this.GetWeaponScore(x)).ToList();

            // pris.isActive = (() => this.<> f__this.ForPrisoners);
            Action draftAction = delegate
                {
                    foreach (Pawn pawn in this.Map.mapPawns.FreeColonistsSpawned)
                    {
                        if (!pawn.IsColonistPlayerControlled)
                        {
                            continue;
                        }

                        if (pawn.Dead || pawn.Downed)
                        {
                            continue;
                        }

                        SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(pawn);
                        pawnSave.armorOnly = true;
                        pawnSave.forceStatUpdate = true;

                        pawn.jobs.StopAll();
                        pawn.drafter.Drafted = true;

                        pawn.mindState.nextApparelOptimizeTick = -5000;

                        Job job = null;
                        Thing thing = null;
                        if (pawn.equipment.Primary == null && !pawn.story.WorkTagIsDisabled(WorkTags.Violent)
                            && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)
                            && !pawn.story.traits.HasTrait(TraitDefOf.Brawler) && !weaponList.NullOrEmpty())
                        {
                            thing = weaponList.Last();

                            // thing = GenClosest.ClosestThingReachable(
                            // pawn.Position,
                            // pawn.Map,
                            // ThingRequest.ForGroup(ThingRequestGroup.Weapon),
                            // PathEndMode.OnCell,
                            // TraverseParms.For(pawn),
                            // 50f,
                            // x => !x.IsForbidden(pawn) && pawn.CanReserve(x) && x.def.IsRangedWeapon,
                            // null,
                            // 0,
                            // 15);
                            if (thing != null)
                            {
                                job = new Job(JobDefOf.Equip, thing);
                                pawn.Reserve(thing, job);
                                weaponList.Remove(weaponList.Last());
                            }
                        }

                        if (job != null)
                        {
                            pawn.jobs.jobQueue.EnqueueFirst(job);
                        }

                        bool ranged = thing != null && thing.def.IsRangedWeapon;

                        pawn.DoApparelJobs(ranged);

                        // foreach (Apparel apparel in pawn.apparel.WornApparel)
                        // {
                        // pawn.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.RemoveApparel, apparel) { haulDroppedApparel = true });
                        // }
                        Job jobby = new Job(DefDatabase<JobDef>.GetNamed("GoToDraftOf"))
                                        {
                                            targetA = this.Position
                                                .RandomAdjacentCell8Way(),
                                            locomotionUrgency =
                                                LocomotionUrgency
                                                    .Sprint
                                        };
                        pawn.jobs.jobQueue.EnqueueLast(jobby);
                        pawnSave.armorOnly = false;
                    }

                    this.DeSpawn();
                };
            draft.action = draftAction;
            yield return draft;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }

            if (this.Spawned && this.ticksToDespawn > 0)
            {
                stringBuilder.AppendLine(
                    "WillDespawnIn".Translate() + ": " + this.ticksToDespawn.TicksToSecondsString());
            }

            return stringBuilder.ToString().TrimEndNewlines();
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

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private bool AlreadySatisfiedWithCurrentWeapon([NotNull] Pawn pawn)
        {
            ThingWithComps primary = pawn.equipment.Primary;
            if (primary == null)
            {
                return false;
            }

            // if (this.preferBuildingDestroyers)
            // {
            // if (!pawn.equipment.PrimaryEq.PrimaryVerb.verbProps.ai_IsBuildingDestroyer)
            // {
            // return false;
            // }
            // }
            // else
            return true;
        }

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private float GetWeaponScore([CanBeNull] Thing wep)
        {
            if (wep == null)
            {
                return 0;
            }

            VerbProperties verbProps = wep.TryGetComp<CompEquippable>().PrimaryVerb.verbProps;
            if (verbProps.ai_IsBuildingDestroyer || verbProps.defaultProjectile.projectile.damageDef == DamageDefOf.Bomb
                || verbProps.defaultProjectile.projectile.damageDef == DamageDefOf.Burn)
            {
                return -1;
            }

            float score = 1f;
            score *= verbProps.range / verbProps.defaultCooldownTime;
            return score;
        }
    }
}