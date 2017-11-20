// Outfitter/ApparelStatsHelper.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2015-12-31 14:34

namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class ApparelStatsHelper
    {
        private const float ScoreFactorIfNotReplacing = 10f;

        private static float NegMax(bool mainJob = false)
        {
            return mainJob ? -9f : -3f;
        }

        private static float PosMax(bool mainJob = false)
        {
            return mainJob ? 9f : 3f;
        }

        private static float NegMed(bool mainJob = false)
        {
            return mainJob ? -6f : -2f;
        }

        private static float PosMed(bool mainJob = false)
        {
            return mainJob ? 6f : 2f;
        }

        private static float PosMin(bool mainJob = false)
        {
            return mainJob ? 3f : 1f;
        }

        private static float NegMin(bool mainJob = false)
        {
            return mainJob ? -3f : -1f;
        }

        // New curve
        public static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
                                                                                  {
                                                                                      new CurvePoint(
                                                                                          0f,
                                                                                          0f),
                                                                                      new CurvePoint(
                                                                                          0.2f,
                                                                                          0.15f),
                                                                                      new CurvePoint(
                                                                                          0.25f,
                                                                                          0.3f),
                                                                                      new CurvePoint(
                                                                                          0.5f,
                                                                                          0.4f),
                                                                                      new CurvePoint(
                                                                                          0.6f,
                                                                                          0.85f),
                                                                                      new CurvePoint(
                                                                                          1f,
                                                                                          1f)

                                                                                      // new CurvePoint( 0.0f, 0.0f ),
                                                                                      // new CurvePoint( 0.25f, 0.15f ),
                                                                                      // new CurvePoint( 0.5f, 0.7f ),
                                                                                      // new CurvePoint( 1f, 1f )
                                                                                  };

        [NotNull]
        private static readonly List<string> IgnoredWorktypeDefs = new List<string>();

        private static readonly Dictionary<Pawn, ApparelStatCache> PawnApparelStatCaches =
            new Dictionary<Pawn, ApparelStatCache>();

        private static List<StatDef> allApparelStats;

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        private static List<StatDef> AllStatDefsModifiedByAnyApparel
        {
            get
            {
                if (allApparelStats.NullOrEmpty())
                {
                    allApparelStats = new List<StatDef>();

                    // add all stat modifiers from all apparels
                    foreach (ThingDef apparel in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.IsApparel))
                    {
                        if (apparel.equippedStatOffsets.NullOrEmpty())
                        {
                            continue;
                        }

                        foreach (StatModifier modifier in apparel.equippedStatOffsets.Where(
                            modifier => !allApparelStats.Contains(modifier.stat)))
                        {
                            allApparelStats.Add(modifier.stat);
                        }
                    }

                    ApparelStatCache.FillIgnoredInfused_PawnStatsHandlers(ref allApparelStats);
                }

                return allApparelStats;
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static float ApparelScoreGain([NotNull] this Pawn pawn, [NotNull] Apparel newAp)
        {
            ApparelStatCache conf = pawn.GetApparelStatCache();

            // get the score of the considered apparel
            float candidateScore = conf.ApparelScoreRaw(newAp, pawn);

            // float candidateScore = StatCache.WeaponScoreRaw(ap, pawn);

            // get the current list of worn apparel
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            bool willReplace = false;

            // check if the candidate will replace existing gear
            foreach (Apparel wornAp in wornApparel.Where(
                apparel => !ApparelUtility.CanWearTogether(apparel.def, newAp.def, pawn.RaceProps.body)))
            {
                // can't drop forced gear
                if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornAp))
                {
                    return -1000f;
                }

                // if replaces, score is difference of the two pieces of gear + penalty
                candidateScore -= conf.ApparelScoreRaw(wornAp, pawn);
                willReplace = true;
            }

            // increase score if this piece can be worn without replacing existing gear.
            if (!willReplace)
            {
                candidateScore *= ScoreFactorIfNotReplacing;
            }

            return candidateScore;
        }

        public static ApparelStatCache GetApparelStatCache([NotNull] this Pawn pawn)
        {
            if (!PawnApparelStatCaches.ContainsKey(pawn))
            {
                PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            }

            return PawnApparelStatCaches[pawn];
        }

        [NotNull]
        public static Dictionary<StatDef, float> GetWeightedApparelArmorStats([NotNull] this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();

            // add weights for all worktypes, multiplied by job priority
            if (pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon)
            {
                foreach (KeyValuePair<StatDef, float> stat in GetStatsOfArmorRanged())
                {
                    float weight = stat.Value;

                    AddStatToDict(stat.Key, weight, ref dict);
                }
            }
            else
            {
                foreach (KeyValuePair<StatDef, float> stat in GetStatsOfArmorMelee())
                {
                    float weight = stat.Value;

                    AddStatToDict(stat.Key, weight, ref dict);
                }
            }

            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 2.5
                    dict[key] /= max / ApparelStatCache.MaxValue;
                }
            }

            return dict;
        }

        [NotNull]
        public static Dictionary<StatDef, float> GetWeightedApparelIndividualStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            SaveablePawn pawnSave = pawn.GetSaveablePawn();

            // dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            // dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);
            if (pawnSave.AddIndividualStats)
            {

                // ReSharper disable once InconsistentNaming
                if (pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicDrone))
                {
                    GameCondition_PsychicEmanation activeCondition =
                        pawn.Map.gameConditionManager.GetActiveCondition<GameCondition_PsychicEmanation>();
                    if (activeCondition.gender == pawn.gender && activeCondition.def.droneLevel > PsychicDroneLevel.None)
                    {
                        switch (pawn.story.traits.DegreeOfTrait(TraitDefOf.PsychicSensitivity))
                        {
                            case -1:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, -0.25f, ref dict);
                                    break;
                                }

                            case 0:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, 0.5f, ref dict);
                                    break;
                                }

                            case 1:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, -0.75f, ref dict);
                                    break;
                                }

                            case 2:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, -1f, ref dict);
                                    break;
                                }
                        }
                    }
                }


                if (pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSoothe))
                {
                    if (pawn.Map.gameConditionManager.GetActiveCondition<GameCondition_PsychicEmanation>().gender
                        == pawn.gender)
                    {
                        switch (pawn.story.traits.DegreeOfTrait(TraitDefOf.PsychicSensitivity))
                        {
                            case -1:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, 1f, ref dict);
                                    break;
                                }

                            case 0:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, 0.75f, ref dict);
                                    break;
                                }

                            case 1:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, 0.5f, ref dict);
                                    break;
                                }

                            case 2:
                                {
                                    AddStatToDict(StatDefOf.PsychicSensitivity, 0.25f, ref dict);
                                    break;
                                }
                        }
                    }
                }

                if (pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout))
                {
                    AddStatToDict(StatDefOf.ToxicSensitivity, -1.5f, ref dict);
                    AddStatToDict(StatDefOf.ImmunityGainSpeed, 1f, ref dict);
                }

                switch (pawn.story.traits.DegreeOfTrait(TraitDefOf.Nerves))
                {
                    case -1:
                        AddStatToDict(StatDefOf.MentalBreakThreshold, -0.75f, ref dict);
                        break;

                    case -2:
                        AddStatToDict(StatDefOf.MentalBreakThreshold, -1.5f, ref dict);
                        break;
                }

                switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("Neurotic")))
                {
                    case 1:
                        AddStatToDict(StatDefOf.MentalBreakThreshold, -0.75f, ref dict);
                        break;

                    case 2:
                        AddStatToDict(StatDefOf.MentalBreakThreshold, -1.5f, ref dict);

                        break;
                }

                if (pawn.story.traits.HasTrait(TraitDefOf.TooSmart))
                {
                    AddStatToDict(StatDefOf.MentalBreakThreshold, -1f, ref dict);
                }

                // float v1 = pawn.GetStatValue(StatDefOf.MentalBreakThreshold);
                // float v2 = pawn.def.GetStatValueAbstract(StatDefOf.MentalBreakThreshold);
                // if (v1 > v2)
                // {
                // AddStatToDict(StatDefOf.MentalBreakThreshold, (-v1 - v2) * 5, ref dict);
                // }
            }

            // No normalizing for indiidual stats
            // if (dict.Count > 0)
            // {
            // // normalize weights
            // float max = dict.Values.Select(Math.Abs).Max();
            // foreach (StatDef key in new List<StatDef>(dict.Keys))
            // {
            // // normalize max of absolute weigths to be 1.5
            // dict[key] /= max / 1.5f;
            // }
            // }
            return dict;
        }

        [NotNull]
        public static Dictionary<StatDef, float> GetWeightedApparelStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            SaveablePawn pawnSave = pawn.GetSaveablePawn();

            // dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            // dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);

            // Adds manual prioritiy adjustments
            if (pawnSave.AddWorkStats)
            {
                // add weights for all worktypes, multiplied by job priority
                List<WorkTypeDef> workTypes = DefDatabase<WorkTypeDef>.AllDefsListForReading
                    .Where(def => pawn.workSettings.WorkIsActive(def)).ToList();

                int maxPriority = workTypes.Aggregate(
                    1,
                    (current, workType) => Mathf.Max(current, pawn.GetWorkPriority(workType)));

                int minPriority = workTypes.Aggregate(
                    1,
                    (current, workType) => Mathf.Min(current, pawn.GetWorkPriority(workType)));

                string log = "Outfitter Priorities, Pawn: " + pawn + " - Max: " + minPriority + "/" + maxPriority;

                foreach (WorkTypeDef workType in workTypes)
                {
                    List<KeyValuePair<StatDef, float>> statsOfWorkType = GetStatsOfWorkType(pawn, workType).ToList();

                    foreach (KeyValuePair<StatDef, float> stat in statsOfWorkType)
                    {
                        int priority = Find.PlaySettings.useWorkPriorities ? pawn.GetWorkPriority(workType) : 3;

                        float priorityAdjust = 1f / priority / maxPriority;

                        if (pawnSave.AddPersonalStats)
                        {
                            for (int i = 0; i < workType.relevantSkills.Count; i++)
                            {
                                SkillDef relSkill = workType.relevantSkills[i];
                                SkillRecord record = pawn.skills.GetSkill(relSkill);
                                float skillMod = 1 + (2f * record.Level / 20);
                                switch (record.passion)
                                {
                                    case Passion.None: break;
                                    case Passion.Minor:
                                        skillMod *= 2f;
                                        break;

                                    case Passion.Major:
                                        skillMod *= 3f;
                                        break;
                                }
                                priorityAdjust *= skillMod;
                            }
                        }

                        float weight = stat.Value * priorityAdjust;

                        AddStatToDict(stat.Key, weight, ref dict);

                        log += "\n" + workType.defName + " - priority " + "-" + priority + " - adjusted "
                               + priorityAdjust;
                    }
                }

                // Log.Message(log);

                // adjustments for traits
                AdjustStatsForTraits(pawn, ref dict);
            }

            float num = ApparelStatCache.MaxValue / 8 * 5;
            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 1.5
                    dict[key] /= max / num;
                }
            }

            return dict;
        }

        [NotNull]
        public static List<StatDef> NotYetAssignedStatDefs([NotNull] this Pawn pawn)
        {
            return AllStatDefsModifiedByAnyApparel
                .Except(pawn.GetApparelStatCache().StatCache.Select(prio => prio.Stat)).ToList();
        }

        private static void AddStatToDict(
            [NotNull] StatDef stat,
            float weight,
            [NotNull] ref Dictionary<StatDef, float> dict)
        {
            if (dict.ContainsKey(stat))
            {
                dict[stat] += weight;
            }
            else
            {
                dict.Add(stat, weight);
            }
        }

        private static void AdjustStatsForTraits(Pawn pawn, [NotNull] ref Dictionary<StatDef, float> dict)
        {
            foreach (StatDef key in new List<StatDef>(dict.Keys))
            {
                if (key == StatDefOf.MoveSpeed)
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("SpeedOffset")))
                    {
                        case -1:
                            dict[key] *= 1.5f;
                            break;

                        case 1:
                            dict[key] *= 0.5f;
                            break;

                        case 2:
                            dict[key] *= 0.25f;
                            break;
                    }
                }

                if (key == StatDefOf.WorkSpeedGlobal)
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDefOf.Industriousness))
                    {
                        case -2:
                            dict[key] *= 2f;
                            break;

                        case -1:
                            dict[key] *= 1.5f;
                            break;

                        case 1:
                            dict[key] *= 0.5f;
                            break;

                        case 2:
                            dict[key] *= 0.25f;
                            break;
                    }
                }
            }
        }

        // RimWorld.ThoughtWorker_PsychicDrone

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfArmorMelee()
        {
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, 2.4f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -2.4f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 1.2f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Heat, 1.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Electric, 1.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 0f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, 0f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 0f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 0f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 0f);
        }

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfArmorRanged()
        {
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 0.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Heat, 1.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Electric, 1.5f);
        }

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType([NotNull] this Pawn pawn,
                                                                                    [NotNull] WorkTypeDef worktype)
        {
            SaveablePawn pawnSave = pawn.GetSaveablePawn();
            bool mainJob = false;
            if (pawnSave.mainJob == MainJob.Soldier00Close_Combat)
            {
                mainJob = true;

                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, NegMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, PosMed());
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, NegMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, PosMax(mainJob));
                yield break;
            }

            if (pawnSave.mainJob == MainJob.Soldier00Ranged_Combat)
            {
                mainJob = true;
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, PosMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, PosMed(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, PosMin());
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, NegMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, NegMax(mainJob));
                yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, PosMax(mainJob));
                yield break;
            }

            switch (worktype.defName)
            {
                case "Firefighter": yield break;

                case "Patient": yield break;

                case "Doctor":
                    if (pawnSave.mainJob == MainJob.Doctor)
                    {
                        mainJob = true;
                    }
                    if (!DefDatabase<WorkTypeDef>.AllDefsListForReading.Any(x => x.defName == "FSFSurgeon"))
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalSurgerySuccessChance, PosMax(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.MedicalOperationSpeed, PosMax(mainJob));
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendQuality, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendSpeed, PosMed(mainJob));
                    yield break;

                case "PatientBedRest": yield break;

                case "Flicker": yield break;

                case "Warden":
                    if (pawnSave.mainJob == MainJob.Warden)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, PosMed(mainJob));
                    yield break;

                case "Handling":
                    if (pawnSave.mainJob == MainJob.Handler)
                    {
                        mainJob = true;
                    }
                    if (!DefDatabase<WorkTypeDef>.AllDefsListForReading.Any(x => x.defName == "FSFTraining"))
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, PosMax(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, PosMax(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, PosMed(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, PosMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, NegMin(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, PosMin(mainJob));
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherYield, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherSpeed, PosMax(mainJob));
                    yield break;
                //  yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, PosMin(mainJob));

                case "Cooking":
                    if (pawnSave.mainJob == MainJob.Cook)
                    {
                        mainJob = true;
                    }

                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.05f);
                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);

                    if (!DefDatabase<WorkTypeDef>.AllDefsListForReading.Any(x => x.defName == "FSFBrewing"))
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.BrewingSpeed, PosMax(mainJob));
                    }
                    if (!DefDatabase<WorkTypeDef>.AllDefsListForReading.Any(x => x.defName == "FSFButcher"))
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshSpeed, PosMax(mainJob));
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshEfficiency, PosMax(mainJob));
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.CookSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, NegMax(mainJob));
                    yield break;

                case "Hunting":
                    if (pawnSave.mainJob == MainJob.Hunter)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, NegMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, NegMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, PosMax(mainJob));
                    yield break;

                case "Construction":
                    if (pawnSave.mainJob == MainJob.Constructor)
                    {
                        mainJob = true;
                    }
                    if (!DefDatabase<WorkTypeDef>.AllDefsListForReading.Any(x => x.defName == "FSFRepair"))
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, PosMax(mainJob));
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructSuccessChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, PosMax(mainJob));
                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Growing":
                    if (pawnSave.mainJob == MainJob.Grower)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantHarvestYield, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Mining":
                    if (pawnSave.mainJob == MainJob.Miner)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningYield, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, PosMax(mainJob));
                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield break;

                case "PlantCutting":
                    //  yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, PosMin(mainJob));
                    //  yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantHarvestYield, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Smithing":
                    if (pawnSave.mainJob == MainJob.Smith)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmithingSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Tailoring":
                    if (pawnSave.mainJob == MainJob.Tailor)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.TailoringSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Art":
                    if (pawnSave.mainJob == MainJob.Artist)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SculptingSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                case "Crafting":
                    if (pawnSave.mainJob == MainJob.Crafter)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmeltingSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidSpeed, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidEfficiency, PosMed(mainJob));
                    yield break;

                case "Hauling":
                    if (pawnSave.mainJob == MainJob.Hauler)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, PosMin(mainJob));
                    yield break;

                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMax(mainJob));
                    yield break;

                case "Research":
                    if (pawnSave.mainJob == MainJob.Researcher)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMin(mainJob));
                    yield break;

                // Colony Manager
                case "Managing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ManagingSpeed"), PosMed(mainJob));
                    yield break;

                // Hospitality
                case "Diplomat":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.DiplomacyPower, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TradePriceImprovement, PosMax(mainJob));
                    yield break;

                // Else
                case "HaulingUrgent": yield break;

                case "FinishingOff": yield break;

                case "Feeding": yield break;

                // FSF Complex Jobs
                case "FSFTraining":
                    if (pawnSave.mainJob == MainJob.Handler)
                    {
                        mainJob = true;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, PosMed(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDPS, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, PosMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, NegMin(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, PosMin(mainJob));

                    yield break;
                case "FSFButcher":
                    if (pawnSave.mainJob == MainJob.Cook)
                    {
                        mainJob = true;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshEfficiency, PosMax(mainJob));
                    yield break;
                case "FSFBrewing":
                    if (pawnSave.mainJob == MainJob.Cook)
                    {
                        mainJob = true;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.BrewingSpeed, PosMax(mainJob));
                    yield break;
                case "FSFRepair":
                    if (pawnSave.mainJob == MainJob.Constructor)
                    {
                        mainJob = true;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, PosMax(mainJob));
                    yield break;
                case "FSFRearming": yield break;
                case "FSFRefueling": yield break;
                case "FSFDrilling":
                    if (pawnSave.mainJob == MainJob.Miner)
                    {
                        mainJob = true;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningYield, PosMax(mainJob));
                    yield break;
                case "FSFDrugs":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMax(mainJob));
                    yield break;
                case "FSFComponents":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMax(mainJob));
                    yield break;
                case "FSFRefining":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, PosMax(mainJob));
                    yield break;
                case "FSFLoading":
                    yield break;
                case "FSFCremating":
                    yield break;
                case "FSFDeconstruct":
                    yield break;
                case "FSFSurgeon":
                    if (pawnSave.mainJob == MainJob.Doctor)
                    {
                        mainJob = true;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalSurgerySuccessChance, PosMax(mainJob));
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.MedicalOperationSpeed, PosMax(mainJob));
                    yield break;


                default:
                    if (!IgnoredWorktypeDefs.Contains(worktype.defName))
                    {
                        Log.Warning("Outfitter: WorkTypeDef " + worktype.defName + " not handled. \nThis is not a bug, just a notice for the mod author.");
                        IgnoredWorktypeDefs.Add(worktype.defName);
                    }

                    yield break;
            }
        }
    }
}