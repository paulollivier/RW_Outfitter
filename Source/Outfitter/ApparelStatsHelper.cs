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
                bool activeDrone = false;

                PsychicDroneLevel psychicDroneLevel = PsychicDroneLevel.None;

                // ReSharper disable once InconsistentNaming
                activeDrone = ExtantShipPart(pawn.Map);

                GameCondition_PsychicEmanation activeCondition =
                    pawn.Map.gameConditionManager.GetActiveCondition<GameCondition_PsychicEmanation>();
                if (activeCondition != null && activeCondition.gender == pawn.gender
                    && activeCondition.def.droneLevel > psychicDroneLevel)
                {
                    activeDrone = true;
                }

                if (activeDrone)
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("PsychicSensitivity")))
                    {
                        case -1:
                            {
                                AddStatToDict(StatDefOf.PsychicSensitivity, -0.25f, ref dict);
                                break;
                            }

                        case 0:
                            {
                                AddStatToDict(StatDefOf.PsychicSensitivity, -0.5f, ref dict);
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

                if (pawn.Map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSoothe))
                {
                    if (pawn.Map.gameConditionManager.GetActiveCondition<GameCondition_PsychicEmanation>().gender
                        == pawn.gender)
                    {
                        switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("PsychicSensitivity")))
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
                //
                // if (v1 > v2)
                // {
                //     AddStatToDict(StatDefOf.MentalBreakThreshold, (-v1 - v2) * 5, ref dict);
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

                var log = "Outfitter Priorities, Pawn: " + pawn + " - Max: " + minPriority + "/" + maxPriority;

                foreach (WorkTypeDef workType in workTypes)
                {
                    foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(pawn, workType))
                    {
                        int priority = Find.PlaySettings.useWorkPriorities ? pawn.GetWorkPriority(workType) : 3;

                        float priorityAdjust = 1f / priority / maxPriority;

                        float weight = stat.Value * priorityAdjust;
                        for (int i = 0; i < workType.relevantSkills.Count; i++)
                        {
                            var relSkill = workType.relevantSkills[i];
                            SkillRecord record = pawn.skills.GetSkill(relSkill);
                            float skillMod = 1 + (record.Level / 20);
                            switch (record.passion)
                            {
                                case Passion.None: break;
                                case Passion.Minor:
                                    skillMod *= 2;
                                    break;
                                case Passion.Major:
                                    skillMod *= 4;
                                    break;
                            }
                            priorityAdjust *= skillMod;
                        }

                        AddStatToDict(stat.Key, weight, ref dict);
                        log += "\n" + workType.defName + " - priority " + "-" + priority + " - adjusted " + priorityAdjust;
                    }
                }
                Log.Message(log);
                // adjustments for traits
                AdjustStatsForTraits(pawn, ref dict);
            }
            var num = ApparelStatCache.MaxValue / 8 * 5;
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
            dict.OrderByDescending(x => x.Value);

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
        private static bool ExtantShipPart([NotNull] Map map)
        {
            List<Thing> list = map.listerThings.ThingsOfDef(ThingDefOf.CrashedPsychicEmanatorShipPart);

            return list.Any();
        }

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfArmorMelee()
        {
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 1.8f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 3f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 2.4f);
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
            yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 1f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 2.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Heat, 1.5f);
            yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Electric, 1.5f);
        }

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(Pawn pawn, WorkTypeDef worktype)
        {
            SaveablePawn pawnSave = pawn.GetSaveablePawn();

            if (pawnSave.mainJob == MainJob.Soldier00Close_Combat)
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 2.4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 1.2f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -2.4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, 3f);
                yield break;
            }

            if (pawnSave.mainJob == MainJob.Soldier00Ranged_Combat)
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 0.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, 3f);
                yield break;
            }

            switch (worktype.defName)
            {
                case "Firefighter": yield break;

                case "Patient": yield break;

                case "Doctor":
                    if (pawnSave.mainJob == MainJob.Doctor)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.MedicalOperationSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalSurgerySuccessChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendQuality, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendSpeed, 1.5f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.MedicalOperationSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalSurgerySuccessChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendQuality, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MedicalTendSpeed, 0.5f);
                    yield break;

                case "PatientBedRest": yield break;

                case "Flicker": yield break;

                case "Warden":
                    if (pawnSave.mainJob == MainJob.Warden)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 1.5f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.5f);
                    yield break;

                case "Handling":
                    if (pawnSave.mainJob == MainJob.Handler)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.25f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.25f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 1f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.9f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherYield, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherSpeed, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeDodgeChance, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherYield, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AnimalGatherSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_CooldownMultiplier, -0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageMultiplier, 0.2f);
                    yield break;

                case "Cooking":
                    if (pawnSave.mainJob == MainJob.Cook)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.CookSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.BrewingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshEfficiency, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, -1.5f);
                        yield break;
                    }

                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.05f);
                    // yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.CookSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.BrewingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryFleshEfficiency, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, -0.5f);
                    yield break;

                case "Hunting":
                    if (pawnSave.mainJob == MainJob.Hunter)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1.5f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -2.4f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, 3f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.MeleeDPS, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -0.8f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PainShockThreshold, 1f);
                    yield break;

                case "Construction":
                    if (pawnSave.mainJob == MainJob.Constructor)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructSuccessChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructSuccessChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingSuccessChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Growing":
                    if (pawnSave.mainJob == MainJob.Grower)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantHarvestYield, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantHarvestYield, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Mining":
                    if (pawnSave.mainJob == MainJob.Miner)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningYield, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningYield, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield break;

                case "PlantCutting":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Smithing":
                    if (pawnSave.mainJob == MainJob.Smith)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmithingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmithingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Tailoring":
                    if (pawnSave.mainJob == MainJob.Tailor)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.TailoringSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.TailoringSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Art":
                    if (pawnSave.mainJob == MainJob.Artist)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.SculptingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SculptingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                case "Crafting":
                    if (pawnSave.mainJob == MainJob.Crafter)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 2.5f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmeltingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidSpeed, 1.5f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidEfficiency, 1.5f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.75f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.SmeltingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf2.ButcheryMechanoidEfficiency, 0.5f);
                    yield break;

                case "Hauling":
                    if (pawnSave.mainJob == MainJob.Hauler)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;

                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.25f);
                    yield break;

                case "Research":
                    if (pawnSave.mainJob == MainJob.Researcher)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield break;
                    }

                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield break;

                // Colony Manager
                case "Managing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ManagingSpeed"), 0.5f);
                    yield break;

                // Hospitality
                case "Diplomat":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.DiplomacyPower, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TradePriceImprovement, 1f);
                    yield break;

                // Else
                case "HaulingUrgent": yield break;

                case "FinishingOff": yield break;

                case "Feeding": yield break;

                default:
                    if (!IgnoredWorktypeDefs.Contains(worktype.defName))
                    {
                        Log.Warning("WorkTypeDef " + worktype.defName + " not handled.");
                        IgnoredWorktypeDefs.Add(worktype.defName);
                    }

                    yield break;
            }
        }
    }
}