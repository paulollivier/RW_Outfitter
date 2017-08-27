// Outfitter/StatCache.cs
//
// Copyright Karel Kroeze, 2016.
//
// Created 2016-01-02 13:58

namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Outfitter.Infused;
    using Outfitter.Textures;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public class ApparelStatCache
    {

        #region Public Fields

        // ReSharper disable once CollectionNeverUpdated.Global
        #endregion Public Fields

        #region Private Fields

        private readonly List<StatPriority> _cache;

        private readonly Pawn _pawn;

        private readonly SaveablePawn pawnSave;

        private List<Apparel> _calculatedApparelItems;

        private List<float> _calculatedApparelScore;

        private int _lastStatUpdate;

        private int _lastTempUpdate;

        private int _lastWeightUpdate;


        #endregion Private Fields

        #region Public Constructors

        public ApparelStatCache(Pawn pawn)
            : this(GameComponent_Outfitter.GetCache(pawn))
        {
        }

        // public NeededWarmth neededWarmth;
        public ApparelStatCache(SaveablePawn saveablePawn)
        {
            this._pawn = saveablePawn.Pawn;
            this.pawnSave = GameComponent_Outfitter.GetCache(this._pawn);
            this._cache = new List<StatPriority>();
            this._lastStatUpdate = -5000;
            this._lastTempUpdate = -5000;
            this._lastWeightUpdate = -5000;
        }

        #endregion Public Constructors

        #region Public Properties

        [NotNull]
        public List<StatPriority> StatCache
        {
            get
            {
                SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this._pawn);

                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - this._lastStatUpdate > Outfitter_JobGiver_OptimizeApparel.ApparelStatCheck || pawnSave.forceStatUpdate)
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

                    // clear auto priorities
                    this._cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    this._cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                    // loop over each (new) stat
                    if (pawnSave.armorOnly)
                    {
                        Dictionary<StatDef, float> updateArmorStats = this._pawn.GetWeightedApparelArmorStats();
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
                    else
                    {
                        Dictionary<StatDef, float> updateAutoPriorities = this._pawn.GetWeightedApparelStats();
                        Dictionary<StatDef, float> updateIndividualPriorities =
                            this._pawn.GetWeightedApparelIndividualStats();

                        foreach (KeyValuePair<StatDef, float> pair in updateIndividualPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this._cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority individual =
                                    new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
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
                    }

                    // update our time check.
                    this._lastStatUpdate = Find.TickManager.TicksGame;
                    pawnSave.forceStatUpdate = false;

                    foreach (StatPriority statPriority in this._cache)
                    {
                        if (statPriority.Assignment != StatAssignment.Automatic
                            && statPriority.Assignment != StatAssignment.Individual)
                        {
                            if (statPriority.Assignment != StatAssignment.Override)
                            {
                                statPriority.Assignment = StatAssignment.Manual;
                            }

                            bool exists = false;
                            foreach (Saveable_Pawn_StatDef stat in pawnSave.Stats)
                            {
                                if (!stat.Stat.Equals(statPriority.Stat))
                                {
                                    continue;
                                }

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

        public FloatRange TargetTemperatures
        {
            get
            {
                this.UpdateTemperatureIfNecessary();
                return this.pawnSave.TargetTemperatures;
            }

            set
            {
                this.pawnSave.TargetTemperatures = value;
                this.pawnSave.TargetTemperaturesOverride = true;
            }
        }

        #endregion Public Properties

        #region Private Properties

        private FloatRange TemperatureWeight
        {
            get
            {
                this.UpdateTemperatureIfNecessary(false, true);
                return this.pawnSave.Temperatureweight;
            }
        }

        #endregion Private Properties

        #region Public Methods

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = 1f;
            float num2 = ap.GetStatValue(StatDefOf.ArmorRating_Sharp)
                         + ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;
            return num + num2 * 1.25f;
        }

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
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.DeleteButton))
                {
                    stat.Delete(pawn);
                    stop_ui = true;
                }
            }

            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.ResetButton))
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
            if (buttonTooltip != string.Empty)
            {
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            }

            TooltipHandler.TipRegion(sliderRect, stat.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }


        public float ApparelScoreRaw([NotNull] Apparel ap, [NotNull] Pawn pawn)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (ap.def.thingClass == typeof(ShieldBelt) && pawn.equipment.Primary?.def.IsRangedWeapon == true)
            {
                return -1f;
            }

            // relevant apparel stats
            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            HashSet<StatDef> statBases = new HashSet<StatDef>();
            HashSet<StatDef> infusedOffsets = new HashSet<StatDef>();


            if (Cache.ApparelEntries.ContainsKey(ap))
            {
                equippedOffsets = Cache.ApparelEntries[ap].equippedOffsets;
                statBases = Cache.ApparelEntries[ap].statBases;
                infusedOffsets = Cache.ApparelEntries[ap].infusedOffsets;
            }
            else
            {
                this.GetStatsOfApparel(ap, ref equippedOffsets, ref statBases);
                this.GetStatsOfApparelInfused(ap, ref infusedOffsets);

                ApparelEntry entry = new ApparelEntry();
                entry.equippedOffsets = equippedOffsets;
                entry.statBases = statBases;
                entry.infusedOffsets = infusedOffsets;

                Cache.ApparelEntries.Add(ap, entry);
            }

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            List<StatPriority> stats = pawn.GetApparelStatCache().StatCache;

            foreach (StatPriority statPriority in stats.Where(statPriority => statPriority != null))
            {
                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = ap.GetStatValue(statPriority.Stat);

                    // add stat to base score before offsets are handled ( the pawn's apparel stat cache always has armors first as it is initialized with it).
                    score += statValue * statPriority.Weight;
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = ap.GetEquippedStatValue(statPriority.Stat) - 1;

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    // DoApparelScoreRaw_PawnStatsHandlers(_pawn, apparel, statPriority.Stat, ref statValue);
                    score += statValue * statPriority.Weight;

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (Cache.InfusedIsActive && infusedOffsets.Contains(statPriority.Stat))
                {
                    // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    float statInfused = 0f;
                    InfusedStats.ApparelScoreRaw_PawnStatsHandlers(ap, statPriority.Stat, ref statInfused);

                    score += statInfused * statPriority.Weight;
                }
            }

            score += ap.GetSpecialApparelScoreOffset();

            score += ApparelScoreRaw_ProtectionBaseStat(ap) * 0.1f;

            // offset for apparel hitpoints
            if (ap.def.useHitPoints)
            {
                float x = ap.HitPoints / (float)ap.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }

            if (ap.WornByCorpse && ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }
            }

            if (false)
            {
                if (ap.TryGetQuality(out QualityCategory cat))
                {
                    switch (cat)
                    {
                        case QualityCategory.Awful:
                            score *= 0.7f;
                            break;

                        case QualityCategory.Shoddy:
                            score *= 0.8f;
                            break;

                        case QualityCategory.Poor:
                            score *= 0.9f;
                            break;

                        case QualityCategory.Normal:
                            score *= 1.0f;
                            break;

                        case QualityCategory.Good:
                            score *= 1.05f;
                            break;

                        case QualityCategory.Superior:
                            score *= 1.1f;
                            break;

                        case QualityCategory.Excellent:
                            score *= 1.15f;
                            break;

                        case QualityCategory.Masterwork:
                            score *= 1.2f;
                            break;

                        case QualityCategory.Legendary:
                            score *= 1.25f;
                            break;

                        default: throw new ArgumentOutOfRangeException();
                    }
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
                    score *= 2f;
                }
            }

            score *= this.ApparelScoreRaw_Temperature(ap);

            return score;
        }

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

            insulationCold += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
            insulationHeat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            if (Cache.InfusedIsActive)
            {
                InfusedStats.ApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Cold, ref insulationCold);
                InfusedStats.ApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Heat, ref insulationHeat);
            }

            // string log = apparel.LabelCap + " - InsCold: " + insulationCold + " - InsHeat: " + insulationHeat + " - TargTemp: "
            // + targetTemperatures + "\nMinComfy: " + minComfyTemperature + " - MaxComfy: "
            // + maxComfyTemperature;

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            if (!this._pawn.apparel.WornApparel.NullOrEmpty())
            {
                if (this._pawn.apparel.WornApparel.Contains(apparel))
                {
                    minComfyTemperature -= insulationCold;
                    maxComfyTemperature -= insulationHeat;
                }
                else
                {
                    List<Apparel> wornApparel = this._pawn.apparel.WornApparel;

                    // check if the candidate will replace existing gear
                    foreach (Apparel ap in wornApparel)
                    {
                        if (!ApparelUtility.CanWearTogether(ap.def, apparel.def))
                        {
                            float insulationColdWorn = ap.GetStatValue(StatDefOf.Insulation_Cold);
                            float insulationHeatWorn = ap.GetStatValue(StatDefOf.Insulation_Heat);

                            insulationColdWorn += ap.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
                            insulationHeatWorn += ap.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);


                            if (Cache.InfusedIsActive)
                            {

                                // offsets on apparel infusions
                                InfusedStats.ApparelScoreRaw_PawnStatsHandlers(
                                    ap,
                                    StatDefOf.Insulation_Cold,
                                    ref insulationColdWorn);
                                InfusedStats.ApparelScoreRaw_PawnStatsHandlers(
                                    ap,
                                    StatDefOf.Insulation_Heat,
                                    ref insulationHeatWorn);
                            }

                            minComfyTemperature -= insulationColdWorn;
                            maxComfyTemperature -= insulationHeatWorn;

                            // Log.Message(apparel +"-"+ insulationColdWorn + "-" + insulationHeatWorn + "-" + minComfyTemperature + "-" + maxComfyTemperature);
                        }
                    }
                }
            }

            // log += "\nBasic stat - MinComfy: " + minComfyTemperature + " - MaxComfy: " + maxComfyTemperature;

            // now for the interesting bit.
            FloatRange temperatureScoreOffset = new FloatRange(0f, 0f);
            FloatRange tempWeight = this.TemperatureWeight;

            // isolation_cold is given as negative numbers < 0 means we're underdressed
            float neededInsulation_Cold = targetTemperatures.min - minComfyTemperature;

            // isolation_warm is given as positive numbers.
            float neededInsulation_Warmth = targetTemperatures.max - maxComfyTemperature;

             //log += "\nWeight: " + tempWeight + " - NeedInsCold: " + neededInsulation_Cold + " - NeedInsWarmth: "
             //+ neededInsulation_Warmth;

            // currently too cold
            if (neededInsulation_Cold < 0)
            {
                temperatureScoreOffset.min += -insulationCold * Math.Abs(tempWeight.min);
            }

            // currently warm enough
            else
            {
                // this gear would make us too cold
                if (insulationCold > neededInsulation_Cold)
                {
                    temperatureScoreOffset.min += (neededInsulation_Cold - insulationCold) * Math.Abs(tempWeight.min);
                }
            }

            // currently too warm
            if (neededInsulation_Warmth > 0)
            {
                temperatureScoreOffset.max += insulationHeat * Math.Abs(tempWeight.max);
            }

            // currently cool enough
            else
            {
                // this gear would make us too warm
                if (insulationHeat < neededInsulation_Warmth)
                {
                    temperatureScoreOffset.max += -(neededInsulation_Warmth - insulationHeat)
                                                  * Math.Abs(tempWeight.max);
                }
            }

            // log += "\nScoreOffsetMin: " + temperatureScoreOffset.min + " - ScoreOffsetMax: "
            // + temperatureScoreOffset.max + " => 1 +" + (temperatureScoreOffset.min + temperatureScoreOffset.max)
            // / 50;
            // Log.Message(log);

            // Punish bad apparel
            temperatureScoreOffset.min *= temperatureScoreOffset.min < 0 ? 2f : 1f;
            temperatureScoreOffset.max *= temperatureScoreOffset.max < 0 ? 2f : 1f;
            return 1 + ((temperatureScoreOffset.min + temperatureScoreOffset.max) / 50);
        }

        public void DIALOG_CalculateApparelScoreGain(Pawn pawn, Apparel apparel, out float gain)
        {
            if (this._calculatedApparelItems == null)
            {
                this.DIALOG_InitializeCalculatedApparelScoresFromWornApparel();
            }

            gain = pawn.ApparelScoreGain(apparel);
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            if (Find.TickManager.TicksGame - this._lastTempUpdate > Outfitter_JobGiver_OptimizeApparel.ApparelStatCheck || force)
            {
                // get desired temperatures
                if (!this.pawnSave.TargetTemperaturesOverride)
                {
                    float temp = GenTemperature.GetTemperatureAtTile(this._pawn.Map.Tile);
                    float lowest = this.LowestTemperatureComing(this._pawn.Map);

                    float minTemp = Mathf.Min(lowest - 5f, temp - 15f);

                    this.pawnSave.TargetTemperatures = new FloatRange(minTemp, temp + 15f);

                    if (this.pawnSave.TargetTemperatures.min >= 12)
                    {
                        this.pawnSave.TargetTemperatures.min = 12;
                    }

                    if (this.pawnSave.TargetTemperatures.max <= 32)
                    {
                        this.pawnSave.TargetTemperatures.max = 32;
                    }

                    this._lastTempUpdate = Find.TickManager.TicksGame;
                }
            }

            if (!this.pawnSave.SetRealComfyTemperatures)
            {
                this.pawnSave.RealComfyTemperatures.min =
                    this._pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin);
                this.pawnSave.RealComfyTemperatures.max =
                    this._pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);
                this.pawnSave.SetRealComfyTemperatures = true;
            }

            if (Find.TickManager.TicksGame - this._lastWeightUpdate > Outfitter_JobGiver_OptimizeApparel.ApparelStatCheck || forceweight)
            {
                FloatRange weight = new FloatRange(1f, 1f);

                if (this.pawnSave.TargetTemperatures.min < this.pawnSave.RealComfyTemperatures.min)
                {
                    weight.min += Math.Abs(
                        (this.pawnSave.TargetTemperatures.min - this.pawnSave.RealComfyTemperatures.min) / 100);
                }

                if (this.pawnSave.TargetTemperatures.max > this.pawnSave.RealComfyTemperatures.max)
                {
                    weight.max += Math.Abs(
                        (this.pawnSave.TargetTemperatures.max - this.pawnSave.RealComfyTemperatures.max) / 100);
                }

                this.pawnSave.Temperatureweight = weight;
                this._lastWeightUpdate = Find.TickManager.TicksGame;
            }
        }

        #endregion Public Methods

        #region Private Methods

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

        private void GetStatsOfApparel(Apparel ap, ref HashSet<StatDef> equippedOffsets, ref HashSet<StatDef> statBases)
        {
            if (ap.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in ap.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            if (ap.def.statBases != null)
            {
                foreach (StatModifier statBase in ap.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }
        }

        private void GetStatsOfApparelInfused(Apparel ap, ref HashSet<StatDef> infusedOffsets)
        {
            if (!Cache.InfusedIsActive)
            {
                return;
            }

            foreach (StatPriority statPriority in this._pawn.GetApparelStatCache().StatCache)
            {
                InfusedStats.ApparelScoreRaw_FillInfusedStat(ap, statPriority.Stat, ref infusedOffsets);
            }
        }

        private float GetTemperature(Twelfth twelfth, [NotNull] Map map)
        {
            return GenTemperature.AverageTemperatureAtTileForTwelfth(map.Tile, twelfth);
        }

        private float LowestTemperatureComing(Map map)
        {
            Twelfth twelfth = GenLocalDate.Twelfth(map);
            float a = this.GetTemperature(twelfth, map);
            for (int i = 0; i < 3; i++)
            {
                twelfth = twelfth.NextTwelfth();
                a = Mathf.Min(a, this.GetTemperature(twelfth, map));
            }

            return Mathf.Min(a, map.mapTemperature.OutdoorTemp);
        }

        #endregion Private Methods

        #region Public Classes

        public class StatPriority
        {

            #region Public Constructors

            public StatPriority(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
            {
                this.Stat = stat;
                this.Weight = priority;
                this.Assignment = assignment;
            }

            public StatPriority(
                KeyValuePair<StatDef, float> statDefWeightPair,
                StatAssignment assignment = StatAssignment.Automatic)
            {
                this.Stat = statDefWeightPair.Key;
                this.Weight = statDefWeightPair.Value;
                this.Assignment = assignment;
            }

            #endregion Public Constructors

            #region Public Properties

            public StatAssignment Assignment { get; set; }

            public StatDef Stat { get; }

            public float Weight { get; set; }

            #endregion Public Properties

            #region Public Methods

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

            #endregion Public Methods

        }

        #endregion Public Classes
    }
}