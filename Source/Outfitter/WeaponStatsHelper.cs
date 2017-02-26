// Outfitter/ApparelStatsHelper.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2015-12-31 14:34

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using static Outfitter.SaveablePawn.MainJob;

namespace Outfitter
{
    public static class WeaponStatsHelper
    {
        private static readonly Dictionary<Pawn, WeaponStatCache> PawnWeaponStatCaches = new Dictionary<Pawn, WeaponStatCache>();
        private const float ScoreFactorIfNotReplacing = 10f;

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        // New curve
        public static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint( 0.0f, 0.05f ),
            new CurvePoint( 0.4f, 0.3f ),
            new CurvePoint( 0.6f, 0.75f ),
            new CurvePoint( 1f, 1f )

        };

        public static WeaponStatCache GetWeaponStatCache(this Pawn pawn)
        {
            if (!PawnWeaponStatCaches.ContainsKey(pawn))
            {
                PawnWeaponStatCaches.Add(pawn, new WeaponStatCache(pawn));
            }
            return PawnWeaponStatCaches[pawn];
        }

        public static Dictionary<StatDef, float> GetWeightedWeaponStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            SaveablePawn pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);

            //       dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            //       dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);


            foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(pawn))
            {

                float weight = stat.Value;

                AddStatToDict(stat.Key, weight, ref dict);
            }


            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 1.5
                    dict[key] /= max / 1.5f;
                }
            }

            return dict;
        }


        private static void AddStatToDict(StatDef stat, float weight, ref Dictionary<StatDef, float> dict)
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




        public static float WeaponScoreGain(Pawn pawn, Thing wep)
        {
            WeaponStatCache conf = new WeaponStatCache(pawn);

            // get the score of the considered apparel
            float candidateScore = conf.WeaponScoreRaw(wep, pawn);
            //    float candidateScore = StatCache.WeaponScoreRaw(wep, pawn);

            // get the current list of worn apparel
            Thing equipeedWeapon = pawn.equipment.Primary;

            // check if the candidate will replace existing gear
            bool willReplace = false;
            if (equipeedWeapon != null)
            {
                {
                    //// can't drop forced gear
                    //if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(equipeedWeapon[i]))
                    //{
                    //    return -1000f;
                    //}

                    // if replaces, score is difference of the two pieces of gear
                    candidateScore -= conf.WeaponScoreRaw(equipeedWeapon, pawn);
                    willReplace = true;
                }
            }


            // increase score if this piece can be worn without replacing existing gear.
            if (!willReplace)
            {
                candidateScore *= ScoreFactorIfNotReplacing;
            }



            return candidateScore;
        }

        private static List<StatDef> _allWeaponStats;

        private static List<StatDef> AllStatDefsModifiedByAnyWeapon
        {
            get
            {
                if (_allWeaponStats == null)
                {
                    _allWeaponStats = new List<StatDef>();

                    // add all stat modifiers from all apparels
                    foreach (ThingDef weapon in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.IsWeapon))
                    {
                        if (weapon.statBases != null && weapon.statBases.Count > 0)
                        {
                            foreach (StatModifier modifier in weapon.statBases)
                            {
                                if (!_allWeaponStats.Contains(modifier.stat))
                                {
                                    _allWeaponStats.Add(modifier.stat);
                                }
                            }
                        }


                        if (weapon.equippedStatOffsets != null && weapon.equippedStatOffsets.Count > 0)
                        {
                            foreach (StatModifier modifier in weapon.equippedStatOffsets)
                            {
                                if (!_allWeaponStats.Contains(modifier.stat))
                                {
                                    _allWeaponStats.Add(modifier.stat);
                                }
                            }
                        }
                    }

                    WeaponStatCache.FillIgnoredInfused_PawnStatsHandlers(ref _allWeaponStats);

                }
                return _allWeaponStats;
            }
        }

        public static List<StatDef> NotYetAssignedWeaponStatDefs(this Pawn pawn)
        {
            return
                AllStatDefsModifiedByAnyWeapon
                    .Except(pawn.GetWeaponStatCache().StatCache.Select(prio => prio.Stat))
                    .ToList();
        }

        private static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(Pawn pawn)
        {
            SaveablePawn pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);

            if (pawnSave.mainJob == Soldier00Close_Combat)
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 2f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -1f);

                yield break;
            }
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 1f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 2f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -1f);
                yield break;
            }


        }
    }
}
