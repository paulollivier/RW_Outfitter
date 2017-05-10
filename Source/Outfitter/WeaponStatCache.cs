// Outfitter/WeaponStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
using Outfitter.Textures;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public enum WeaponStatAssignment
    {
        Manual,
        Override,
        Individual,
        Automatic
    }

    public class WeaponStatCache
    {
        private readonly List<StatPriority> _cache;

        private readonly Pawn _pawn;
        private int _lastStatUpdate;
        private int _lastTempUpdate;
        private int _lastWeightUpdate;


        public List<StatPriority> StatCache
        {
            get
            {
                CompOutfit pawnSave = MapComponent_Outfitter.Get.GetCache(_pawn);

                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - _lastStatUpdate > 1900 || pawnSave.forceStatUpdate)
                {
                    // list of auto stats

                    if (_cache.Count < 1 && pawnSave.WeaponStats.Count > 0)
                        foreach (Saveable_Pawn_StatDef vari in pawnSave.WeaponStats)
                        {
                            _cache.Add(new StatPriority(vari.Stat, vari.Weight, vari.Assignment));
                        }
                    pawnSave.WeaponStats.Clear();

                    Dictionary<StatDef, float> updateAutoPriorities = _pawn.GetWeightedWeaponStats();
                    // clear auto priorities
                    _cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    _cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                    // loop over each (new) stat

                    foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                    {
                        // find index of existing priority for this stat
                        int i = _cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            _cache.Add(new StatPriority(pair));
                        }
                        else
                        {
                            // it exists, make sure existing is (now) of type override.
                            _cache[i].Assignment = StatAssignment.Override;
                        }
                    }

                    // update our time check.
                    _lastStatUpdate = Find.TickManager.TicksGame;
                    pawnSave.forceStatUpdate = false;
                }


                foreach (StatPriority statPriority in _cache)
                {
                    if (statPriority.Assignment != StatAssignment.Automatic && statPriority.Assignment != StatAssignment.Individual)
                    {
                        if (statPriority.Assignment != StatAssignment.Override)
                            statPriority.Assignment = StatAssignment.Manual;

                        bool exists = false;
                        foreach (Saveable_Pawn_StatDef stat in pawnSave.WeaponStats)
                        {
                            if (!stat.Stat.Equals(statPriority.Stat)) continue;
                            stat.Weight = statPriority.Weight;
                            stat.Assignment = statPriority.Assignment;
                            exists = true;
                        }
                        if (!exists)
                        {
                            Saveable_Pawn_StatDef stats = new Saveable_Pawn_StatDef
                            {
                                Stat = statPriority.Stat,
                                Assignment = statPriority.Assignment,
                                Weight = statPriority.Weight
                            };
                            pawnSave.WeaponStats.Add(stats);
                        }
                    }
                }

                return _cache;
            }
        }

        // ReSharper disable once CollectionNeverUpdated.Global
        public static HashSet<StatDef> infusedOffsets;


        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Thing wep, StatDef statDef, ref float num);
        public delegate void ApparelScoreRawInfusionHandlers(Pawn pawn, Thing wep, StatDef statDef);
        public delegate void ApparelScoreRawIgnored_WTHandlers(ref List<StatDef> statDef);

        public static event ApparelScoreRawStatsHandler ApparelScoreRaw_PawnStatsHandlers;
        public static event ApparelScoreRawInfusionHandlers ApparelScoreRaw_InfusionHandlers;
        public static event ApparelScoreRawIgnored_WTHandlers Ignored_WTHandlers;

        public static void DoApparelScoreRaw_PawnStatsHandlers(Pawn pawn, Thing wep, StatDef statDef, ref float num)
        {
            ApparelScoreRaw_PawnStatsHandlers?.Invoke(pawn, wep, statDef, ref num);
        }

        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> _allWeaponStats)
        {
            Ignored_WTHandlers?.Invoke(ref _allWeaponStats);
        }

        public static void FillInfusionHashset_PawnStatsHandlers(Pawn pawn, Thing wep, StatDef statDef)
        {
            ApparelScoreRaw_InfusionHandlers?.Invoke(pawn, wep, statDef);
        }

        public WeaponStatCache(CompOutfit compOutfit)
        {
            _pawn = compOutfit.Pawn;
            _cache = new List<StatPriority>();
            _lastStatUpdate = -5000;
            _lastTempUpdate = -5000;
            _lastWeightUpdate = -5000;
        }

        public float WeaponScoreRaw(Thing wep, Pawn pawn)
        {
            // relevant wep stats
            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            if (wep.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in wep.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }
            
            HashSet<StatDef> statBases = new HashSet<StatDef>();
            if (wep.def.statBases != null)
            {
                foreach (StatModifier statBase in wep.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }

            infusedOffsets = new HashSet<StatDef>();
            foreach (StatPriority statPriority in _pawn.GetWeaponStatCache().StatCache)
                FillInfusionHashset_PawnStatsHandlers(_pawn, wep, statPriority.Stat);

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the wep

            foreach (StatPriority statPriority in pawn.GetWeaponStatCache().StatCache)
            {

                // statbases, e.g. armor
                if (statPriority == null)
                    continue;

                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = wep.GetStatValue(statPriority.Stat);

                    // add stat to base score before offsets are handled ( the pawn's wep stat cache always has armors first as it is initialized with it).

                    score += statValue * statPriority.Weight;
                }


                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = GetEquippedStatValue(wep, statPriority.Stat) - 1;
                    //  statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    //DoApparelScoreRaw_PawnStatsHandlers(_pawn, wep, statPriority.Stat, ref statValue);

                    score += statValue * statPriority.Weight;


                    // multiply score to favour items with multiple offsets
                    //     score *= adjusted;

                    //debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(statPriority.Stat))
                {
                    //  float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    float statInfused = 0f;
                    DoApparelScoreRaw_PawnStatsHandlers(_pawn, wep, statPriority.Stat, ref statInfused);

                    score += statInfused * statPriority.Weight;
                }
                //        Debug.LogWarning(statPriority.Stat.LabelCap + " infusion: " + score);

            }

            // offset for wep hitpoints 
            if (wep.def.useHitPoints)
            {
                float x = wep.HitPoints / (float)wep.MaxHitPoints;
                score = score * 0.25f + score * 0.75f * ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            return score;
        }

        public static float GetEquippedStatValue(Thing wep, StatDef stat)
        {

            float baseStat = wep.GetStatValue(stat);
            float currentStat = baseStat + wep.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            //            currentStat += wep.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            //   if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            //   {
            //       return wep.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            //   }

            if (baseStat != 0)
            {
                currentStat = currentStat / baseStat;
            }

            return currentStat;
        }

        private List<Thing> _calculatedWeaponItems;
        private List<float> _calculatedWeaponScore;

        public static void DrawWeaponStatRow(ref Vector2 cur, float width, StatPriority stat, Pawn pawn, out bool stop_ui)
        {
            // sent a signal if the statlist has changed
            stop_ui = false;

            // set up rects
            Rect labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f, 30f);
            Rect sliderRect = new Rect(labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f);
            Rect buttonRect = new Rect(sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f);

            // draw label
            Text.Font = Text.CalcHeight(stat.Stat.LabelCap, labelRect.width) > labelRect.height
                ? GameFont.Tiny
                : GameFont.Small;
            switch (stat.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;
                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;
                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;
                case StatAssignment.Override:
                    GUI.color = new Color(0.75f, 0.75f, 0.75f);
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }
            Widgets.Label(labelRect, stat.Stat.LabelCap);
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            string buttonTooltip = String.Empty;
            if (stat.Assignment == StatAssignment.Manual)
            {
                buttonTooltip = "StatPriorityDelete".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.deleteButton))
                {
                    stat.Delete(pawn);
                    stop_ui = true;
                }
            }
            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.resetButton))
                {
                    stat.Reset(pawn);
                    stop_ui = true;
                }
            }

            // draw line behind slider
            GUI.color = new Color(.3f, .3f, .3f);
            for (int y = (int)cur.y; y < cur.y + 30; y += 5)
            {
                Widgets.DrawLineVertical((sliderRect.xMin + sliderRect.xMax) / 2f, y, 3f);
            }

            // draw slider 
            switch (stat.Assignment)
            {
                case StatAssignment.Automatic:
                    GUI.color = Color.grey;
                    break;
                case StatAssignment.Individual:
                    GUI.color = Color.cyan;
                    break;
                case StatAssignment.Manual:
                    GUI.color = Color.white;
                    break;
                case StatAssignment.Override:
                    GUI.color = new Color(0.8f, 0.8f, 0.8f);
                    break;
                default:
                    GUI.color = Color.white;
                    break;
            }
            float weight = GUI.HorizontalSlider(sliderRect, stat.Weight, -2.5f, 2.5f);
            if (Mathf.Abs(weight - stat.Weight) > 1e-4)
            {
                stat.Weight = weight;
                if (stat.Assignment == StatAssignment.Automatic || stat.Assignment == StatAssignment.Individual)
                {
                    stat.Assignment = StatAssignment.Override;
                }
            }
            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion(labelRect, stat.Stat.LabelCap + "\n\n" + stat.Stat.description);
            if (buttonTooltip != String.Empty)
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            TooltipHandler.TipRegion(sliderRect, stat.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }

        public class StatPriority
        {
            public StatDef Stat { get; }
            public StatAssignment Assignment { get; set; }
            public float Weight { get; set; }

            public StatPriority(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
            {
                Stat = stat;
                Weight = priority;
                Assignment = assignment;
            }

            public StatPriority(KeyValuePair<StatDef, float> statDefWeightPair, StatAssignment assignment = StatAssignment.Automatic)
            {
                Stat = statDefWeightPair.Key;
                Weight = statDefWeightPair.Value;
                Assignment = assignment;
            }

            public void Delete(Pawn pawn)
            {
                pawn.GetWeaponStatCache()._cache.Remove(this);

                CompOutfit pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);
                pawnSave.WeaponStats.RemoveAll(i => i.Stat == Stat);
            }

            public void Reset(Pawn pawn)
            {
                Dictionary<StatDef, float> stats = pawn.GetWeightedWeaponStats();

                if (stats.ContainsKey(Stat))
                {
                    Weight = stats[Stat];
                    Assignment = StatAssignment.Automatic;
                }


                CompOutfit pawnSave = pawn.TryGetComp<CompOutfit>();
                pawnSave.WeaponStats.RemoveAll(i => i.Stat == Stat);
            }
        }

        public bool CalculateApparelScoreGain(Apparel apparel, out float gain)
        {
            if (_calculatedWeaponItems == null)
                DIALOG_InitializeCalculatedApparelScoresFromWornApparel();

            return CalculateApparelScoreGain(apparel, WeaponScoreRaw(apparel, _pawn), out gain);
        }

        private bool CalculateApparelScoreGain(Apparel apparel, float score, out float candidateScore)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (apparel.def == ThingDefOf.Apparel_ShieldBelt &&
                 _pawn.equipment.Primary != null &&
                 !_pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                candidateScore = -1000f;
                return false;
            }

            // get the score of the considered wep
            candidateScore = score;
            //    float candidateScore = StatCache.WeaponScoreRaw(ap, pawn);



            // check if the candidate will replace existing gear
            bool willReplace = false;
            for (int i = 0; i < _calculatedWeaponItems.Count; i++)
            {
                Thing wornApparel = _calculatedWeaponItems[i];
                {
                    // get the current list of worn wep
                    // can't drop forced gear

                    candidateScore -= _calculatedWeaponScore[i]; //+= ???? -= old
                    willReplace = true;
                }
            }

            candidateScore += apparel.GetSpecialApparelScoreOffset();


            // increase score if this piece can be worn without replacing existing gear.
            if (!willReplace)
            {
                candidateScore *= 10f;
            }

            return true;
        }


        private void DIALOG_InitializeCalculatedApparelScoresFromWornApparel()
        {
            _calculatedWeaponItems = new List<Thing>();
            _calculatedWeaponScore = new List<float>();
            foreach (Apparel apparel in _pawn.apparel.WornApparel)
            {
                _calculatedWeaponItems.Add(apparel);
                _calculatedWeaponScore.Add(WeaponScoreRaw(apparel, _pawn));
            }
        }




    }


}