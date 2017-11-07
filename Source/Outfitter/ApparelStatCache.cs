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

    using Outfitter.Textures;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public partial class ApparelStatCache
    {
      //  public List<Apparel> recentApparel = new List<Apparel>();

        public readonly List<StatPriority> Cache;

        public const float MaxValue = 2.5f;

        private readonly Pawn pawn;

        private readonly SaveablePawn pawnSave;

        private int lastStatUpdate;

        private int lastTempUpdate;

        private int lastWeightUpdate;

        public ApparelStatCache(Pawn pawn)
            : this(GameComponent_Outfitter.GetCache(pawn))
        {
        }

        // public NeededWarmth neededWarmth;
        public ApparelStatCache([NotNull] SaveablePawn saveablePawn)
        {
            this.pawn = saveablePawn.Pawn;
            this.pawnSave = GameComponent_Outfitter.GetCache(this.pawn);
            this.Cache = new List<StatPriority>();
            this.lastStatUpdate = -5000;
            this.lastTempUpdate = -5000;
            this.lastWeightUpdate = -5000;
        }

        public delegate void ApparelScoreRawIgnored_WTHandlers(ref List<StatDef> statDef);

        public delegate void ApparelScoreRawInfusionHandlers(
            [NotNull] Apparel apparel,
            [NotNull] StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets);

        public delegate void ApparelScoreRawStatsHandler(Apparel apparel, StatDef statDef, out float num);

        public static event ApparelScoreRawInfusionHandlers ApparelScoreRaw_FillInfusedStat;

        public static event ApparelScoreRawStatsHandler ApparelScoreRaw_PawnStatsHandlers;

        public static event ApparelScoreRawIgnored_WTHandlers Ignored_WTHandlers;

        [NotNull]
        public List<StatPriority> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - this.lastStatUpdate > JobGiver_OptimizeApparel.ApparelStatCheck
                    || this.pawnSave.forceStatUpdate)
                {
                    // list of auto stats
                    if (this.Cache.Count < 1 && this.pawnSave.Stats.Count > 0)
                    {
                        foreach (Saveable_Pawn_StatDef statDef in this.pawnSave.Stats)
                        {
                            this.Cache.Add(new StatPriority(statDef.Stat, statDef.Weight, statDef.Assignment));
                        }
                    }

                    this.pawnSave.Stats.Clear();

                    // clear auto priorities
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);
                    this.Cache.RemoveAll(stat => stat.Assignment == StatAssignment.Individual);

                    // loop over each (new) stat
                    if (this.pawnSave.armorOnly)
                    {
                        Dictionary<StatDef, float> updateArmorStats = this.pawn.GetWeightedApparelArmorStats();
                        foreach (KeyValuePair<StatDef, float> pair in updateArmorStats)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority armorStats = new StatPriority(pair.Key, pair.Value);
                                this.Cache.Add(armorStats);
                            }
                            else
                            {
                                // it exists, make sure existing is (now) of type override.
                                this.Cache[i].Weight += pair.Value;
                            }
                        }
                    }
                    else
                    {
                        Dictionary<StatDef, float> updateAutoPriorities = this.pawn.GetWeightedApparelStats();
                        Dictionary<StatDef, float> updateIndividualPriorities =
                            this.pawn.GetWeightedApparelIndividualStats();

                        foreach (KeyValuePair<StatDef, float> pair in updateIndividualPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                StatPriority individual =
                                    new StatPriority(pair.Key, pair.Value, StatAssignment.Individual);
                                this.Cache.Add(individual);
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }

                        foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                        {
                            // find index of existing priority for this stat
                            int i = this.Cache.FindIndex(stat => stat.Stat == pair.Key);

                            // if index -1 it doesnt exist yet, add it
                            if (i < 0)
                            {
                                this.Cache.Add(new StatPriority(pair));
                            }
                            else
                            {
                                // if exists, make sure existing is (now) of type override.
                                this.Cache[i].Assignment = StatAssignment.Override;
                            }
                        }
                    }

                    // update our time check.
                    this.lastStatUpdate = Find.TickManager.TicksGame;
                    this.pawnSave.forceStatUpdate = false;

                    foreach (StatPriority statPriority in this.Cache.Where(
                        statPriority => statPriority.Assignment != StatAssignment.Automatic
                                        && statPriority.Assignment != StatAssignment.Individual))
                    {
                        bool exists = false;
                        foreach (Saveable_Pawn_StatDef stat in this.pawnSave.Stats.Where(
                            stat => stat.Stat.Equals(statPriority.Stat)))
                        {
                            stat.Weight = statPriority.Weight;
                            stat.Assignment = statPriority.Assignment;
                            exists = true;
                        }

                        if (exists)
                        {
                            continue;
                        }

                        Saveable_Pawn_StatDef stats =
                            new Saveable_Pawn_StatDef
                            {
                                Stat = statPriority.Stat,
                                Assignment = statPriority.Assignment,
                                Weight = statPriority.Weight
                            };
                        this.pawnSave.Stats.Add(stats);
                    }
                }

                return this.Cache;
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

        private FloatRange TemperatureWeight
        {
            get
            {
                this.UpdateTemperatureIfNecessary(false, true);
                return this.pawnSave.Temperatureweight;
            }
        }

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = 1f;
            float num2 = ap.GetStatValue(StatDefOf.ArmorRating_Sharp)
                         + ap.GetStatValue(StatDefOf.ArmorRating_Blunt) * 0.75f;
            return num + num2 * 1.25f;
        }

        public static void DoApparelScoreRaw_PawnStatsHandlers([NotNull] Apparel apparel, [NotNull] StatDef statDef, out float num)
        {
            num = 0f;
            ApparelScoreRaw_PawnStatsHandlers?.Invoke(apparel, statDef, out num);
        }

        public static void DrawStatRow(
            ref Vector2 cur,
            float width,
            [NotNull] StatPriority stat,
            Pawn pawn,
            out bool stopUI)
        {
            // sent a signal if the statlist has changed
            stopUI = false;

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
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
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
                    stopUI = true;
                }
            }

            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.ResetButton))
                {
                    stat.Reset(pawn);
                    stopUI = true;
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
                    GUI.color = new Color(0.75f, 0.69f, 0.33f);
                    break;

                default:
                    GUI.color = Color.white;
                    break;
            }
            float weight = GUI.HorizontalSlider(sliderRect, stat.Weight, -MaxValue, MaxValue);
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

        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> allApparelStats)
        {
            Ignored_WTHandlers?.Invoke(ref allApparelStats);
        }

        public float ApparelScoreRaw([NotNull] Apparel ap, [NotNull] Pawn pawn)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (ap.def.thingClass == typeof(ShieldBelt) && pawn.equipment.Primary?.def.IsRangedWeapon == true)
            {
                return -1f;
            }

            // relevant apparel stats
            ApparelEntry entry = this.GetAllOffsets(ap);

            HashSet<StatDef> equippedOffsets = entry.equippedOffsets;
            HashSet<StatDef> statBases = entry.statBases;
            HashSet<StatDef> infusedOffsets = entry.infusedOffsets;

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
                    float statValue = ap.GetEquippedStatValue(statPriority.Stat);

                    score += statValue * statPriority.Weight;

                    // multiply score to favour items with multiple offsets
                    // score *= adjusted;

                    // debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(statPriority.Stat))
                {
                    // float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);

                    DoApparelScoreRaw_PawnStatsHandlers(ap, statPriority.Stat, out float statInfused);

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

            if (ap.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (ThoughtUtility.CanGetThought(pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }
            }

            score *= this.ApparelScoreRaw_Temperature(ap);

            return score;
        }

        public float ApparelScoreRaw_Temperature([NotNull] Apparel apparel)
        {
            // float minComfyTemperature = pawnSave.RealComfyTemperatures.min;
            // float maxComfyTemperature = pawnSave.RealComfyTemperatures.max;
            float minComfyTemperature = this.pawn.ComfortableTemperatureRange().min;
            float maxComfyTemperature = this.pawn.ComfortableTemperatureRange().max;

            // temperature
            FloatRange targetTemperatures = this.TargetTemperatures;

            // offsets on apparel
            float insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            float insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            insulationCold += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
            insulationHeat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);
            {
                // offsets on apparel infusions
                DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Cold, out float infInsulationCold);
                DoApparelScoreRaw_PawnStatsHandlers(apparel, StatDefOf.Insulation_Heat, out float infInsulationHeat);
                insulationCold += infInsulationCold;
                insulationHeat += infInsulationHeat;
            }

            // string log = apparel.LabelCap + " - InsCold: " + insulationCold + " - InsHeat: " + insulationHeat + " - TargTemp: "
            // + targetTemperatures + "\nMinComfy: " + minComfyTemperature + " - MaxComfy: "
            // + maxComfyTemperature;

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            List<Apparel> wornApparel = this.pawn.apparel.WornApparel;
            if (!wornApparel.NullOrEmpty())
            {
                if (wornApparel.Contains(apparel))
                {
                    minComfyTemperature -= insulationCold;
                    maxComfyTemperature -= insulationHeat;
                }
                else
                {
                    // check if the candidate will replace existing gear
                    foreach (Apparel ap in wornApparel)
                    {
                        if (!ApparelUtility.CanWearTogether(ap.def, apparel.def, this.pawn.RaceProps.body))
                        {
                            float insulationColdWorn = ap.GetStatValue(StatDefOf.Insulation_Cold);
                            float insulationHeatWorn = ap.GetStatValue(StatDefOf.Insulation_Heat);

                            insulationColdWorn +=
                                ap.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Cold);
                            insulationHeatWorn +=
                                ap.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.Insulation_Heat);
                            {
                                // offsets on apparel infusions
                                DoApparelScoreRaw_PawnStatsHandlers(
                                    ap,
                                    StatDefOf.Insulation_Cold,
                                    out float infInsulationColdWorn);
                                DoApparelScoreRaw_PawnStatsHandlers(
                                    ap,
                                    StatDefOf.Insulation_Heat,
                                    out float infInsulationHeatWorn);
                                insulationColdWorn += infInsulationColdWorn;
                                insulationHeatWorn += infInsulationHeatWorn;
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

            // log += "\nWeight: " + tempWeight + " - NeedInsCold: " + neededInsulation_Cold + " - NeedInsWarmth: "
            // + neededInsulation_Warmth;

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
            return 1 + (temperatureScoreOffset.min + temperatureScoreOffset.max) / 25;
        }

        public void UpdateTemperatureIfNecessary(bool force = false, bool forceweight = false)
        {
            if (Find.TickManager.TicksGame - this.lastTempUpdate > JobGiver_OptimizeApparel.ApparelStatCheck || force)
            {
                // get desired temperatures
                if (!this.pawnSave.TargetTemperaturesOverride)
                {
                    float temp = GenTemperature.GetTemperatureAtTile(this.pawn.Map.Tile);
                    float lowest = this.LowestTemperatureComing(this.pawn.Map);

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

                    this.lastTempUpdate = Find.TickManager.TicksGame;
                }
            }

            if (!this.pawnSave.SetRealComfyTemperatures)
            {
                this.pawnSave.RealComfyTemperatures.min =
                    this.pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
                this.pawnSave.RealComfyTemperatures.max =
                    this.pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);
                this.pawnSave.SetRealComfyTemperatures = true;

                // this.pawnSave.RealComfyTemperatures.min =
                //     this.pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin);
                // this.pawnSave.RealComfyTemperatures.max =
                //     this.pawn.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);
                // this.pawnSave.SetRealComfyTemperatures = true;
            }

            if (Find.TickManager.TicksGame - this.lastWeightUpdate > JobGiver_OptimizeApparel.ApparelStatCheck
                || forceweight)
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
                this.lastWeightUpdate = Find.TickManager.TicksGame;
            }
        }

        private static void FillInfusionHashset_PawnStatsHandlers(
            Apparel apparel,
            StatDef parentStat,
            ref HashSet<StatDef> infusedOffsets)
        {
            ApparelScoreRaw_FillInfusedStat?.Invoke(apparel, parentStat, ref infusedOffsets);
        }

        public ApparelEntry GetAllOffsets([NotNull] Apparel ap)
        {
            if (Outfitter.Cache.ApparelEntries.ContainsKey(ap))
            {
                return Outfitter.Cache.ApparelEntries[ap];
            }

            ApparelEntry entry = new ApparelEntry();
            this.GetStatsOfApparel(ap, ref entry.equippedOffsets, ref entry.statBases);
            this.GetStatsOfApparelInfused(ap, ref entry.infusedOffsets);

            Outfitter.Cache.ApparelEntries.Add(ap, entry);
            return entry;
        }

        private void GetStatsOfApparel([NotNull] Apparel ap, ref HashSet<StatDef> equippedOffsets, ref HashSet<StatDef> statBases)
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
            foreach (StatPriority statPriority in this.pawn.GetApparelStatCache().StatCache)
            {
                FillInfusionHashset_PawnStatsHandlers(ap, statPriority.Stat, ref infusedOffsets);
            }
        }

        private float GetTemperature(Twelfth twelfth, [NotNull] Map map)
        {
            return GenTemperature.AverageTemperatureAtTileForTwelfth(map.Tile, twelfth);
        }

        private float LowestTemperatureComing([NotNull] Map map)
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

        // ReSharper disable once CollectionNeverUpdated.Global
    }
}