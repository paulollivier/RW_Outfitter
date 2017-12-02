using static UnityEngine.GUILayout;

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

    public class Window_Pawn_ApparelDetail : Verse.Window
    {
        private const float baseValue = 85f;

        private readonly Apparel apparel;

        private readonly GUIStyle fontBold =
            new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                normal = {
                                textColor = Color.white
                             },
                padding = new RectOffset(0, 0, 12, 6)
            };

        private readonly GUIStyle headline =
            new GUIStyle
            {
                fontStyle = FontStyle.Bold,
                fontSize = 16,
                normal = {
                                textColor = Color.white
                             },
                padding = new RectOffset(0, 0, 12, 6)
            };

        private readonly GUIStyle hoverBox = new GUIStyle { hover = { background = OutfitterTextures.BgColor } };

        private readonly Pawn pawn;

        private readonly GUIStyle whiteLine = new GUIStyle { normal = { background = OutfitterTextures.White } };

        private Vector2 _scrollPosition;

        private Def def;

        private ThingDef stuff;

        public Window_Pawn_ApparelDetail(Pawn pawn, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            this.preventCameraMotion = false;

            this.pawn = pawn;
            this.apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(510f, 550f);

        [CanBeNull]
        private Def Def
        {
            get
            {
                if (this.apparel != null)
                {
                    return this.apparel.def;
                }

                return this.def;
            }
        }

        private bool IsVisible
        {
            get
            {
                // thing selected is a pawn
                if (this.SelPawn == null)
                {
                    return false;
                }

                // of this colony
                if (this.SelPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (this.SelPawn.apparel == null)
                {
                    return false;
                }

                return true;
            }
        }

        private Pawn SelPawn => Find.Selector.SingleSelectedThing as Pawn;

        public override void DoWindowContents(Rect inRect)
        {
            ApparelStatCache conf = this.pawn.GetApparelStatCache();

            Rect conRect = new Rect(inRect);

            conRect.height -= 50f;

            BeginArea(conRect);

            // begin main group
            BeginVertical();

            Label(this.GetTitle(), this.headline);
            Text.Font = GameFont.Small;

            // GUI.BeginGroup(contentRect);
            float labelWidth = conRect.width - baseValue - baseValue - baseValue - 48f;

            this.DrawLine("Status", labelWidth, "BaseMod", "Strength", "Score", this.fontBold);

            Space(6f);
            Label(string.Empty, this.whiteLine, Height(1));
            Space(6f);

            ApparelEntry apparelEntry = conf.GetAllOffsets(this.apparel);

            HashSet<StatDef> equippedOffsets = apparelEntry.equippedOffsets;
            HashSet<StatDef> statBases = apparelEntry.statBases;
            HashSet<StatDef> infusedOffsets = apparelEntry.infusedOffsets;

            this._scrollPosition = BeginScrollView(this._scrollPosition, Width(conRect.width));

            // relevant apparel stats

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            foreach (StatPriority statPriority in this.pawn.GetApparelStatCache().StatCache
                .OrderBy(i => i.Stat.LabelCap))
            {
                StatDef stat = statPriority.Stat;
                string statLabel = stat.LabelCap;

                // statbases, e.g. armor

                // StatCache.DoApparelScoreRaw_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat, ref currentStat);
                if (statBases.Contains(stat))
                {
                    float statValue = this.apparel.GetStatValue(stat);
                    float statScore = 0f;
                    if (ApparelStatCache.specialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(this.apparel, statPriority, this.pawn, statValue, ref statScore);
                    }
                    else
                    {
                        // statValue += StatCache.StatInfused(infusionSet, statPriority, ref baseInfused);
                        statScore = statValue * statPriority.Weight;
                    }

                    score += statScore;

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));
                }

                if (equippedOffsets.Contains(stat))
                {
                    float statValue = this.apparel.GetEquippedStatValue(this.pawn, stat);

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    float statScore = 0f;
                    if (ApparelStatCache.specialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(this.apparel, statPriority, this.pawn, statValue, ref statScore);
                    }
                    else
                    {
                        statScore = statValue * statPriority.Weight;
                    }

                    score += statScore;

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));
                }

                if (infusedOffsets.Contains(stat))
                {
                    GUI.color = Color.green; // new Color(0.5f, 1f, 1f, 1f);

                    // float statInfused = StatCache.StatInfused(infusionSet, statPriority, ref dontcare);
                    ApparelStatCache.DoApparelScoreRaw_PawnStatsHandlers(this.apparel, stat, out float statValue);

                    bool flag = true;

                    float statScore = 0f;
                    if (ApparelStatCache.specialStats.Contains(stat))
                    {
                        ApparelStatCache.CalculateScoreForSpecialStats(
                            this.apparel,
                            statPriority,
                            this.pawn,
                            statValue,
                            ref statScore);
                    }
                    else
                    {
                        // Bug with Infused and "Ancient", it completely kills the pawn's armor
                        if (statValue < 0
                            && (stat == StatDefOf.ArmorRating_Blunt || stat == StatDefOf.ArmorRating_Sharp))
                        {
                            score = -2f;
                            flag = false;
                        }

                        statScore = statValue * statPriority.Weight;
                    }

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToStringPercent("N1"),
                        statPriority.Weight.ToString("N2"),
                        statScore.ToString("N2"));

                    GUI.color = Color.white;

                    if (flag)
                    {
                        score += statScore;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            GUI.color = Color.white;

            // end upper group
            EndScrollView();

            // begin lower group
            FlexibleSpace();
            Space(6f);
            Label(string.Empty, this.whiteLine, Height(1));
            Space(6f);
            this.DrawLine(string.Empty, labelWidth, "Modifier", string.Empty, "Subtotal");

            this.DrawLine("BasicStatusOfApparel".Translate(), labelWidth, "1.00", "+", score.ToString("N2"));

            float special = this.apparel.GetSpecialApparelScoreOffset();
            if (Math.Abs(special) > 0f)
            {
                score += special;

                this.DrawLine(
                    "OutfitterSpecialScore".Translate(),
                    labelWidth,
                    special.ToString("N2"),
                    "+",
                    score.ToString("N2"));
            }

            float armor = ApparelStatCache.ApparelScoreRaw_ProtectionBaseStat(this.apparel);

            if (Math.Abs(armor) > 0.01f)
            {
                score += armor;

                this.DrawLine("OutfitterArmor".Translate(), labelWidth, armor.ToString("N2"), "+", score.ToString("N2"));
            }

            if (this.apparel.def.useHitPoints)
            {
                // durability on 0-1 scale
                float x = this.apparel.HitPoints / (float)this.apparel.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);

                this.DrawLine(
                    "OutfitterHitPoints".Translate(),
                    labelWidth,
                    x.ToString("N2"),
                    "weighted",
                    score.ToString("N2"));

                GUI.color = Color.white;
            }

            if (this.apparel.WornByCorpse && ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.DeadMansApparel))
            {
                score -= 0.5f;
                if (score > 0f)
                {
                    score *= 0.1f;
                }

                this.DrawLine(
                    "OutfitterWornByCorpse".Translate(),
                    labelWidth,
                    "modified",
                    "weighted",
                    score.ToString("N2"));
            }

            float mod = 1f;

            if (this.apparel.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }

                this.DrawLine(
                    "OutfitterHumanLeather".Translate(),
                    labelWidth,
                    "modified",
                    "weighted",
                    score.ToString("N2"));
            }

            float temperature = conf.ApparelScoreRaw_Temperature(this.apparel);

            if (Math.Abs(temperature - 1f) > 0)
            {
                score *= temperature;

                this.DrawLine(
                    "OutfitterTemperature".Translate(),
                    labelWidth,
                    temperature.ToString("N2"),
                    "*",
                    score.ToString("N2"));
            }

            this.DrawLine(
                "OutfitterTotal".Translate(),
                labelWidth,
                string.Empty,
                "=",
                conf.ApparelScoreRaw(this.apparel).ToString("N2"));

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            // end main group
            EndVertical();
            EndArea();
        }

        public override void WindowUpdate()
        {
            if (!this.IsVisible)
            {
                this.Close(false);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            MainTabWindow_Inspect inspectWorker = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
            this.windowRect = new Rect(
                770f,
                inspectWorker.PaneTopY - 30f - this.InitialSize.y,
                this.InitialSize.x,
                this.InitialSize.y).Rounded();
        }

        private void DrawLine(
            string statDefLabelText,
            float statDefLabelWidth,
            string statDefValueText,
            string multiplierText,
            string finalValueText,
            GUIStyle style = null)
        {
            if (style != null)
            {
                BeginHorizontal(style);
            }
            else
            {
                BeginHorizontal(this.hoverBox);
            }

            Label(statDefLabelText, Width(statDefLabelWidth));
            Label(statDefValueText, Width(baseValue));
            Label(multiplierText, Width(baseValue));
            Label(finalValueText, Width(baseValue));
            EndHorizontal();

            // Text.Anchor = TextAnchor.UpperLeft;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefLabelWidth, itemRect.height), statDefLabelText);
            // itemRect.xMin += statDefLabelWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefValueWidth, itemRect.height), statDefValueText);
            // itemRect.xMin += statDefValueWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, multiplierWidth, itemRect.height), multiplierText);
            // itemRect.xMin += multiplierWidth;
            // Text.Anchor = TextAnchor.UpperRight;
            // Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, finalValueWidth, itemRect.height), finalValueText);
            // itemRect.xMin += finalValueWidth;
        }

        private string GetTitle()
        {
            if (this.apparel != null)
            {
                return this.apparel.LabelCap;
            }

            ThingDef thingDef = this.Def as ThingDef;
            if (thingDef != null)
            {
                return GenLabel.ThingLabel(thingDef, this.stuff).CapitalizeFirst();
            }

            return this.Def.LabelCap;
        }

#pragma warning disable 649
#pragma warning restore 649
#pragma warning disable 649
#pragma warning restore 649
    }
}