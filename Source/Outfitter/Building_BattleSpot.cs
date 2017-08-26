namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using JetBrains.Annotations;

    using RimWorld;

    using Verse;
    using Verse.AI;

    [StaticConstructorOnStartup]

    // ReSharper disable once UnusedMember.Global
    public class Building_BattleSpot : Building
    {
        #region Private Fields

        private const float MinScoreGainToCare = 0.09f;

        private int ticksToDespawn;

        #endregion Private Fields

        #region Private Properties

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

        #endregion Private Properties

        #region Public Methods

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.ticksToDespawn, "ticksToDespawn");
        }

        public void GetApparelList([NotNull] Pawn pawn, [NotNull] out List<Thing> apparelList)
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

            float num = 0f;
            List<Thing> list = this.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);

            if (list.Count == 0)
            {
                return;
            }

            foreach (Thing t in list)
            {
                Apparel apparel = (Apparel)t;
                if (currentOutfit.filter.Allows(apparel))
                {
                    if (this.Map.slotGroupManager.SlotGroupAt(apparel.Position) != null)
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
                                        if (pawn.CanReserveAndReach(
                                            apparel,
                                            PathEndMode.OnCell,
                                            pawn.NormalMaxDanger()))
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
            foreach (KeyValuePair<Thing, float> apparelThing in myList)
            {
                bool change = true;
                if (!apparelList.NullOrEmpty())
                {
                    foreach (Thing ap in apparelList)
                    {
                        if (ApparelUtility.CanWearTogether(ap.def, apparelThing.Key.def))
                        {
                            continue;
                        }

                        // No gain no change
                        change = false;
                    }
                }

                if (change)
                {
                    foreach (Apparel apparel in pawn.apparel.WornApparel)
                    {
                        if (!ApparelUtility.CanWearTogether(apparel.def, (apparelThing.Key as Apparel).def))
                        {
                            if (pawn.ApparelScoreGain(apparel) > apparelThing.Value)
                            {
                                apparelList.Add(apparelThing.Key);
                            }
                        }
                    }
                }
            }
        }

        // ReSharper disable once MethodTooLong
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
            Action draftAction = delegate
                {
                    Outfitter_JobGiver_OptimizeApparel.dropped.Clear();
                    Outfitter_JobGiver_OptimizeApparel.reserved.Clear();

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

                        pawn.mindState.nextApparelOptimizeTick = -5000;

                        for (int i = 0; i < 15; i++)
                        {
                            Job outfitJob = pawn.GetApparel();
                            if (outfitJob != null)
                            {
                                pawn.jobs.jobQueue.EnqueueLast(outfitJob);
                            }
                            else
                            {
                                break;
                            }
                        }

                        if (!this.AlreadySatisfiedWithCurrentWeapon(pawn)
                            && !pawn.story.WorkTagIsDisabled(WorkTags.Violent)
                            && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                        {
                            Thing thing = GenClosest.ClosestThingReachable(
                                pawn.Position,
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

        #endregion Public Methods

        #region Private Methods

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
        private float GetWeaponScore([NotNull] Pawn pawn, [CanBeNull] Thing wep)
        {
            if (wep == null)
            {
                return 0;
            }

            if (wep.def.IsMeleeWeapon && wep.GetStatValue(StatDefOf.MeleeWeapon_DamageAmount)
                < this.MinMeleeWeaponDamageThreshold)
            {
                return -1;
            }

            if (wep.TryGetComp<CompEquippable>().PrimaryVerb.verbProps.ai_IsIncendiary || wep
                    .TryGetComp<CompEquippable>().PrimaryVerb.verbProps.ai_IsBuildingDestroyer)
            {
                return -1;
            }

            int melee = pawn.skills.GetSkill(SkillDefOf.Melee).Level;
            int shooter = pawn.skills.GetSkill(SkillDefOf.Shooting).Level;

            if (shooter > melee)
            {
                if (wep.def.IsMeleeWeapon)
                {
                    if (pawn.story.traits.HasTrait(TraitDefOf.Brawler))
                    {
                        return 2 * pawn.skills.GetSkill(SkillDefOf.Melee).Level;
                    }

                    return -1;
                }
            }

            if (wep.def.IsRangedWeapon)
            {
                return 2 * pawn.skills.GetSkill(SkillDefOf.Shooting).Level;
            }

            return 1 * pawn.skills.GetSkill(SkillDefOf.Melee).Level;
        }

        // RimWorld.JobGiver_PickUpOpportunisticWeapon
        private bool ShouldEquip([NotNull] Thing newWep, [NotNull] Pawn pawn)
        {
            return this.GetWeaponScore(pawn, newWep) > this.GetWeaponScore(pawn, pawn.equipment.Primary);
        }

        #endregion Private Methods
    }
}