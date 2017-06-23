// Outfitter/StatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using Outfitter.Textures;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public enum StatAssignment
    {
        Manual,
        Override,
        Individual,
        Automatic
    }

    public class ApparelStatCache
    {
        private readonly List<StatPriority> _cache;

        private readonly Pawn _pawn;
        private int _lastStatUpdate;
        private int _lastTempUpdate;
        private int _lastWeightUpdate;

        private FloatRange TemperatureWeight
        {
            get
            {
                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);
                this.UpdateTemperatureIfNecessary(false, true);
                return pawnSave.Temperatureweight;
            }
        }

        public FloatRange TargetTemperatures
        {
            get
            {
                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);

                this.UpdateTemperatureIfNecessary();
                return pawnSave.TargetTemperatures;
            }

            set
            {
                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);
                pawnSave.TargetTemperatures = value;
                pawnSave.TargetTemperaturesOverride = true;
            }
        }

        public List<StatPriority> StatCache
        {
            get
            {
                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);

                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - this._lastStatUpdate > 1900 || pawnSave.forceStatUpdate)
                {
                    // list of auto stats
                    if (this._cache.Count < 1 && pawnSave.Stats.Count > 0)
                    {
                        foreach (Saveable_Pawn_StatDef vari in pawnSave.Stats)
                        {
                            this._cache.Add(new StatPriority(vari.Stat, vari.Weight, vari.Assignment));
                        }
                    }

                    pawnSave.Stats.Clear();

                    Dictionary<StatDef, float> updateAutoPriorities = this._pawn.GetWeightedApparelStats();
                    Dictionary<StatDef, float> updateIndividualPriorities = this._pawn.GetWeightedApparelIndividualStats();

                    Dictionary<StatDef, float> updateArmorStats = this._pawn.GetWeightedApparelArmorStats();

                    // clear auto priorities
                    this._cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    this._cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);




                    // loop over each (new) stat
                    foreach (KeyValuePair<StatDef, float> pair in updateIndividualPriorities)
                    {
                        // find index of existing priority for this stat
                        int i = this._cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            StatPriority individual = new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
                            this._cache.Add(individual);
                        }
                        else
                        {
                            // it exists, make sure existing is (now) of type override.
                            this._cache[i].Assignment = StatAssignment.Override;
                        }
                    }

                    foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                    {
                        // find index of existing priority for this stat
                        int i = this._cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            this._cache.Add(new StatPriority(pair));
                        }
                        else
                        {
                            // it exists, make sure existing is (now) of type override.
                            this._cache[i].Assignment = StatAssignment.Override;
                        }
                    }

                    if (pawnSave.armorOnly)
                    {
                        foreach (KeyValuePair<StatDef, float> pair in updateArmorStats)
                        {
                            // find index of existing priority for this stat
                            int i = this._cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority armorStats = new StatPriority(pair.Key, pair.Value);
                                this._cache.Add(armorStats);
                            }
                            else
                            {
                                // it exists, make sure existing is (now) of type override.
                                this._cache[i].Weight = pair.Value;
                            }
                        }
                    }

                    // update our time check.
                    this._lastStatUpdate = Find.TickManager.TicksGame;
                    pawnSave.forceStatUpdate = false;
                    pawnSave.armorOnly = false;

                    foreach (StatPriority statPriority in this._cache)
                    {
                        if (statPriority.Assignment != StatAssignment.Automatic
                            && statPriority.Assignment != StatAssignment.Individual)
                        {
                            if (statPriority.Assignment != StatAssignment.Override)
                                statPriority.Assignment = StatAssignment.Manual;

                            bool exists = false;
                            foreach (Saveable_Pawn_StatDef stat in pawnSave.Stats)
                            {
                                if (!stat.Stat.Equals(statPriority.Stat)) continue;
                                stat.Weight = statPriority.Weight;
                                stat.Assignment = statPriority.Assignment;
                                exists = true;
                            }

                            if (!exists)
                            {
                                Saveable_Pawn_StatDef stats =
                                    new Saveable_Pawn_StatDef
                                    {
                                        Stat = statPriority.Stat,
                                        Assignment = statPriority.Assignment,
                                        Weight = statPriority.Weight
                                    };
                                pawnSave.Stats.Add(stats);
                            }
                        }
                    }
                }

                return this._cache;
            }
        }

        // ReSharper disable once CollectionNeverUpdated.Global
        public static HashSet<StatDef> infusedOffsets;


        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef statDef, ref float num);
        public delegate void ApparelScoreRawInfusionHandlers(Pawn pawn, Apparel apparel, StatDef statDef);
        public delegate void ApparelScoreRawIgnored_WTHandlers(ref List<StatDef> statDef);

        public static event ApparelScoreRawStatsHandler ApparelScoreRaw_PawnStatsHandlers;
        public static event ApparelScoreRawInfusionHandlers ApparelScoreRaw_InfusionHandlers;
        public static event ApparelScoreRawIgnored_WTHandlers Ignored_WTHandlers;

        public static void DoApparelScoreRaw_PawnStatsHandlers(Pawn pawn, Apparel apparel, StatDef statDef, ref float num)
        {
            ApparelScoreRaw_PawnStatsHandlers?.Invoke(pawn, apparel, statDef, ref num);
        }

        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> _allApparelStats)
        {
            Ignored_WTHandlers?.Invoke(ref _allApparelStats);
        }

        public static void FillInfusionHashset_PawnStatsHandlers(Pawn pawn, Apparel apparel, StatDef statDef)
        {
            ApparelScoreRaw_InfusionHandlers?.Invoke(pawn, apparel, statDef);
        }

        public ApparelStatCache(Pawn pawn)
            : this(GameComponent_Outfitter.GetCache(pawn))
        {
        }

        public ApparelStatCache(SaveablePawn saveablePawn)
        {
            this._pawn = saveablePawn.Pawn;
            this._cache = new List<StatPriority>();
            this._lastStatUpdate = -5000;
            this._lastTempUpdate = -5000;
            this._lastWeightUpdate = -5000;
        }

        public float ApparelScoreRaw(Apparel ap, Pawn pawn, bool armorOnly = false)
        {
            // relevant apparel stats
            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            if (ap.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in ap.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            HashSet<StatDef> statBases = new HashSet<StatDef>();
            if (ap.def.statBases != null)
            {
                foreach (StatModifier statBase in ap.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }

            infusedOffsets = new HashSet<StatDef>();
            foreach (StatPriority statPriority in this._pawn.GetApparelStatCache().StatCache)
            {
                FillInfusionHashset_PawnStatsHandlers(this._pawn, ap, statPriority.Stat);
            }

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            var stats = pawn.GetApparelStatCache().StatCache;

            foreach (StatPriority statPriority in stats)
            {
                // statbases, e.g. armor
                if (statPriority == null)
                {
                    continue;
                }

                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = ap.GetStatValue(statPriority.Stat);

                    // add stat to base score before offsets are handled ( the pawn's apparel stat cache always has armors first as it is initialized with it).
                    score += statValue * statPriority.Weight;
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = GetEquippedStatValue(ap, statPriority.Stat) - 1;

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    // DoApparelScoreRaw_PawnStatsHandlers(_pawn, apparel, statPriority.Stat, ref statValue);
                    score += statValue * statPriority.Weight;

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(statPriority.Stat))
                {
                    // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    float statInfused = 0f;
                    DoApparelScoreRaw_PawnStatsHandlers(this._pawn, ap, statPriority.Stat, ref statInfused);

                    score += statInfused * statPriority.Weight;
                }

                // Debug.LogWarning(statPriority.Stat.LabelCap + " infusion: " + score);
            }

            score += ap.GetSpecialApparelScoreOffset();

            score += this.ApparelScoreRaw_Temperature(ap);

            score += 0.05f * ApparelScoreRaw_ProtectionBaseStat(ap);

            // offset for apparel hitpoints 
            if (ap.def.useHitPoints)
            {
                float x = ap.HitPoints / (float)ap.MaxHitPoints;
                score = score * 0.25f + score * 0.75f * ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            if (ap.WornByCorpse && (pawn == null || ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.DeadMansApparel)))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }

            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (pawn == null || ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (pawn != null && ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score += 0.12f;
                }
            }

            return score;
        }

        public static float GetEquippedStatValue(Apparel apparel, StatDef stat)
        {
            float baseStat = apparel.GetStatValue(stat);
            float currentStat = baseStat + apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);

            // currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            // if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            // {
            // return apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            // }
            if (baseStat != 0)
            {
                currentStat = currentStat / baseStat;
            }

            return currentStat;
        }

        /*
        public float ApparelScoreRaw_InsulationColdAdjust(Apparel ap)
        {
            switch (_neededWarmth)
            {
                case NeededWarmth.Warm:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Cold, null);
                        float final = InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValueAbstract);
                        return final;
                    }

                case NeededWarmth.Cool:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Heat, null);
                        float final = InsulationWarmScoreFactorCurve_NeedCold.Evaluate(statValueAbstract);
                        return final;
                    }
                    
                default:
                    return 1;
            }
        }
*/
        public float ApparelScoreRaw_Temperature(Apparel apparel)
        {
            // float minComfyTemperature = pawnSave.RealComfyTemperatures.min;
            // float maxComfyTemperature = pawnSave.RealComfyTemperatures.max;
            float minComfyTemperature = this._pawn.ComfortableTemperatureRange().min;
            float maxComfyTemperature = this._pawn.ComfortableTemperatureRange().max;

            // temperature
            FloatRange targetTemperatures = this.TargetTemperatures;

            // offsets on apparel
            float insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            float insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            DoApparelScoreRaw_PawnStatsHandlers(this._pawn, apparel, StatDefOf.ComfyTemperatureMin, ref insulationCold);
            DoApparelScoreRaw_PawnStatsHandlers(this._pawn, apparel, StatDefOf.ComfyTemperatureMax, ref insulationHeat);

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            if (this._pawn.apparel.WornApparel.Contains(apparel))
            {
                List<Apparel> wornApparel = this._pawn.apparel.WornApparel;

                // check if the candidate will replace existing gear
                foreach (Apparel ap in wornApparel)
                {
                    if (!ApparelUtility.CanWearTogether(ap.def, apparel.def))
                    {
                        float insulationColdWorn = ap.GetStatValue(StatDefOf.Insulation_Cold);
                        float insulationHeatWorn = ap.GetStatValue(StatDefOf.Insulation_Heat);

                        // offsets on apparel infusions
                        DoApparelScoreRaw_PawnStatsHandlers(
                            this._pawn,
                            ap,
                            StatDefOf.ComfyTemperatureMin,
                            ref insulationColdWorn);
                        DoApparelScoreRaw_PawnStatsHandlers(
                            this._pawn,
                            ap,
                            StatDefOf.ComfyTemperatureMax,
                            ref insulationHeatWorn);

                        minComfyTemperature -= insulationColdWorn;
                        maxComfyTemperature -= insulationHeatWorn;

                        // Log.Message(apparel +"-"+ insulationColdWorn + "-" + insulationHeatWorn + "-" + minComfyTemperature + "-" + maxComfyTemperature);
                    }
                }

                minComfyTemperature -= insulationCold;
                maxComfyTemperature -= insulationHeat;
            }

            // now for the interesting bit.
            float temperatureScoreOffset = 0f;
            FloatRange tempWeight = this.TemperatureWeight;

            // isolation_cold is given as negative numbers < 0 means we're underdressed
            float neededInsulation_Cold = targetTemperatures.min - minComfyTemperature;

            // isolation_warm is given as positive numbers.
            float neededInsulation_Warmth = targetTemperatures.max - maxComfyTemperature;

            // currently too cold
            if (neededInsulation_Cold < 0)
            {
                temperatureScoreOffset += -insulationCold * Math.Abs(tempWeight.min);
            }

            // currently warm enough
            else
            {
                // this gear would make us too cold
                if (insulationCold > neededInsulation_Cold)
                {
                    temperatureScoreOffset += (neededInsulation_Cold - insulationCold) * Math.Abs(tempWeight.min);
                }
            }

            // currently too warm
            if (neededInsulation_Warmth > 0)
            {
                temperatureScoreOffset += insulationHeat * Math.Abs(tempWeight.max);
            }

            // currently cool enough
            else
            {
                // this gear would make us too warm
                if (insulationHeat < neededInsulation_Warmth)
                {
                    temperatureScoreOffset += -(neededInsulation_Warmth - insulationHeat) * Math.Abs(tempWeight.max);
                }
            }

            return temperatureScoreOffset / 10;
        }

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = 1f;
            float num2 = ap.GetStatValue(StatDefOf.ArmorRating_Sharp) + ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;
            return num + num2 * 1.25f;
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);
            if (Find.TickManager.TicksGame - this._lastTempUpdate > 1900 || force)
            {
                // get desired temperatures
                if (!pawnSave.TargetTemperaturesOverride)
                {
                    float temp = this._pawn.Map.mapTemperature.OutdoorTemp;

                    pawnSave.TargetTemperatures = new FloatRange(Math.Max(temp - 7.5f, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min(temp + 7.5f, ApparelStatsHelper.MinMaxTemperatureRange.max));

                    if (pawnSave.TargetTemperatures.min >= 12)
                        pawnSave.TargetTemperatures.min = 12;

                    if (pawnSave.TargetTemperatures.max <= 32)
                        pawnSave.TargetTemperatures.max = 32;

                    this._lastTempUpdate = Find.TickManager.TicksGame;
                }

                // pawnSave.Temperatureweight = GenTemperature.OutdoorTemperatureAcceptableFor(_pawn.def) ? 0.1f : 1f;
            }

            if (!pawnSave.SetRealComfyTemperatures)
            {
                pawnSave.RealComfyTemperatures.min = this._pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin);
                pawnSave.RealComfyTemperatures.max = this._pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);

                // if (_pawn.story.traits.DegreeOfTrait(TraitDef.Named("TemperaturePreference")) != 0)
                // {
                // //calculating trait offset because there's no way to get comfytemperaturemin without clothes
                // List<Trait> traitList = (
                // from trait in _pawn.story.traits.allTraits
                // where trait.CurrentData.statOffsets != null && trait.CurrentData.statOffsets.Any(se => se.stat == StatDefOf.ComfyTemperatureMin || se.stat == StatDefOf.ComfyTemperatureMax)
                // select trait
                // ).ToList();
                // foreach (Trait t in traitList)
                // {
                // pawnSave.RealComfyTemperatures.min += t.CurrentData.statOffsets.First(se => se.stat == StatDefOf.ComfyTemperatureMin).value;
                // pawnSave.RealComfyTemperatures.max += t.CurrentData.statOffsets.First(se => se.stat == StatDefOf.ComfyTemperatureMax).value;
                // }
                // }
                pawnSave.SetRealComfyTemperatures = true;
            }

            if (Find.TickManager.TicksGame - this._lastWeightUpdate > 1900 || forceweight)
            {
                FloatRange weight = new FloatRange(0f, 0f);

                if (pawnSave.TargetTemperatures.min < pawnSave.RealComfyTemperatures.min)
                {
                    weight.min -= Math.Abs((pawnSave.TargetTemperatures.min - pawnSave.RealComfyTemperatures.min) / 25);
                }

                if (pawnSave.TargetTemperatures.max > pawnSave.RealComfyTemperatures.max)
                {
                    weight.max += Math.Abs((pawnSave.TargetTemperatures.max - pawnSave.RealComfyTemperatures.max) / 25);
                }

                pawnSave.Temperatureweight = weight;
                this._lastWeightUpdate = Find.TickManager.TicksGame;
            }

        }

        private List<Apparel> _calculatedApparelItems;
        private List<float> _calculatedApparelScore;

        public static void DrawStatRow(ref Vector2 cur, float width, StatPriority stat, Pawn pawn, out bool stop_ui)
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
            string buttonTooltip = string.Empty;
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
            if (buttonTooltip != string.Empty) TooltipHandler.TipRegion(buttonRect, buttonTooltip);
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
                this.Stat = stat;
                this.Weight = priority;
                this.Assignment = assignment;
            }

            public StatPriority(KeyValuePair<StatDef, float> statDefWeightPair, StatAssignment assignment = StatAssignment.Automatic)
            {
                this.Stat = statDefWeightPair.Key;
                this.Weight = statDefWeightPair.Value;
                this.Assignment = assignment;
            }

            public void Delete(Pawn pawn)
            {
                pawn.GetApparelStatCache()._cache.Remove(this);

                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(pawn);
                pawnSave.Stats.RemoveAll(i => i.Stat == this.Stat);
            }

            public void Reset(Pawn pawn)
            {
                Dictionary<StatDef, float> stats = pawn.GetWeightedApparelStats();
                Dictionary<StatDef, float> indiStats = pawn.GetWeightedApparelIndividualStats();

                if (stats.ContainsKey(this.Stat))
                {
                    this.Weight = stats[this.Stat];
                    this.Assignment = StatAssignment.Automatic;
                }

                if (indiStats.ContainsKey(this.Stat))
                {
                    this.Weight = indiStats[this.Stat];
                    this.Assignment = StatAssignment.Individual;
                }

                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(pawn);
                pawnSave.Stats.RemoveAll(i => i.Stat == this.Stat);
            }
        }

        public bool CalculateApparelScoreGain(Apparel apparel, out float gain)
        {
            if (this._calculatedApparelItems == null) this.DIALOG_InitializeCalculatedApparelScoresFromWornApparel();

            return this.CalculateApparelScoreGain(apparel, this.ApparelScoreRaw(apparel, this._pawn), out gain);
        }

        private bool CalculateApparelScoreGain(Apparel apparel, float score, out float candidateScore)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (apparel.def == ThingDefOf.Apparel_ShieldBelt && this._pawn.equipment.Primary != null
                && !this._pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                candidateScore = -1000f;
                return false;
            }

            // get the score of the considered apparel
            candidateScore = score;

            // float candidateScore = StatCache.WeaponScoreRaw(ap, pawn);

            // check if the candidate will replace existing gear
            bool willReplace = false;
            for (int i = 0; i < this._calculatedApparelItems.Count; i++)
            {
                Apparel wornApparel = this._calculatedApparelItems[i];
                if (!ApparelUtility.CanWearTogether(wornApparel.def, apparel.def))
                {
                    // get the current list of worn apparel
                    // can't drop forced gear
                    if (!this._pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel))
                    {
                        return false;
                    }

                    candidateScore -= this._calculatedApparelScore[i]; // += ???? -= old
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
            this._calculatedApparelItems = new List<Apparel>();
            this._calculatedApparelScore = new List<float>();
            foreach (Apparel apparel in this._pawn.apparel.WornApparel)
            {
                this._calculatedApparelItems.Add(apparel);
                this._calculatedApparelScore.Add(this.ApparelScoreRaw(apparel, this._pawn));
            }
        }




    }


}