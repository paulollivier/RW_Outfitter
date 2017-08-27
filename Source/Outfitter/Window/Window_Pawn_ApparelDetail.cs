using static UnityEngine.GUILayout;

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

    public class Window_Pawn_ApparelDetail : Verse.Window
    {
        #region Public Constructors

        public Window_Pawn_ApparelDetail(Pawn pawn, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            this.preventCameraMotion = false;

            this.pawn = pawn;
            this.apparel = apparel;
        }

        #endregion Public Constructors

        #region Public Properties

        public override Vector2 InitialSize => new Vector2(510f, 550f);

        #endregion Public Properties

        #region Protected Methods

        protected override void SetInitialSizeAndPosition()
        {
            MainTabWindow_Inspect inspectWorker = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
            this.windowRect = new Rect(
                770f,
                inspectWorker.PaneTopY - 30f - this.InitialSize.y,
                this.InitialSize.x,
                this.InitialSize.y).Rounded();
        }

        #endregion Protected Methods

        #region Private Fields

        private readonly Apparel apparel;

        private readonly Pawn pawn;

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

        private readonly GUIStyle whiteLine = new GUIStyle { normal = { background = OutfitterTextures.White } };

        private Vector2 _scrollPosition;

        private Def def;

        private ThingDef stuff;

        #endregion Private Fields

        #region Private Properties

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

        #endregion Private Properties

        #region Public Methods

        public override void DoWindowContents(Rect windowRect)
        {
            ApparelStatCache conf = new ApparelStatCache(this.pawn);

            Rect conRect = new Rect(windowRect);

            conRect.height -= 50f;

            BeginArea(conRect);

            // begin main group
            BeginVertical();

            Label(this.GetTitle(), this.headline);
            Text.Font = GameFont.Small;

            // GUI.BeginGroup(contentRect);
            float baseValue = 85f;
            float multiplierWidth = 85f;
            float finalValue = 85f;
            float labelWidth = conRect.width - baseValue - multiplierWidth - finalValue - 48f;

            this.DrawLine(
                "Status",
                labelWidth,
                "Base",
                baseValue,
                "Strength",
                multiplierWidth,
                "Score",
                finalValue,
                this.fontBold);

            Space(6f);
            Label(string.Empty, this.whiteLine, Height(1));
            Space(6f);

            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            HashSet<StatDef> statBases = new HashSet<StatDef>();
            HashSet<StatDef> infusedOffsets = new HashSet<StatDef>();

            Dictionary<Apparel, ApparelEntry> apparelEntries = Cache.ApparelEntries;

            if (apparelEntries.ContainsKey(this.apparel))
            {
                equippedOffsets = apparelEntries[this.apparel].equippedOffsets;
                statBases = apparelEntries[this.apparel].statBases;
                infusedOffsets = apparelEntries[this.apparel].infusedOffsets;
            }

            this._scrollPosition = BeginScrollView(this._scrollPosition, Width(conRect.width));

            // relevant apparel stats

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel
            foreach (ApparelStatCache.StatPriority statPriority in this.pawn.GetApparelStatCache().StatCache
                .OrderBy(i => i.Stat.LabelCap))
            {
                string statLabel = statPriority.Stat.LabelCap;

                // statbases, e.g. armor

                // StatCache.DoApparelScoreRaw_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat, ref currentStat);
                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = this.apparel.GetStatValue(statPriority.Stat);

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref baseInfused);
                    float statScore = statValue * statPriority.Weight;
                    score += statScore;

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToString("N2"),
                        baseValue,
                        statPriority.Weight.ToString("N2"),
                        multiplierWidth,
                        statScore.ToString("N2"),
                        finalValue);
                }

                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = this.apparel.GetEquippedStatValue(statPriority.Stat) - 1;

                    // statValue += StatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    float statScore = statValue * statPriority.Weight;
                    score += statScore;

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToString("N2"),
                        baseValue,
                        statPriority.Weight.ToString("N2"),
                        multiplierWidth,
                        statScore.ToString("N2"),
                        finalValue);
                }

                GUI.color = Color.white;
            }

            foreach (ApparelStatCache.StatPriority statPriority in this.pawn.GetApparelStatCache().StatCache
                .OrderBy(i => i.Stat.LabelCap))
            {
                GUI.color = Color.green; // new Color(0.5f, 1f, 1f, 1f);
                string statLabel = statPriority.Stat.LabelCap;

                if (Cache.InfusedIsActive&&infusedOffsets.Contains(statPriority.Stat))
                {
                    // float statInfused = StatCache.StatInfused(infusionSet, statPriority, ref dontcare);
                    float statValue = 0f;
                    InfusedStats.ApparelScoreRaw_PawnStatsHandlers(
                        this.apparel,
                        statPriority.Stat,
                        ref statValue);

                    float statScore = statValue * statPriority.Weight;

                    this.DrawLine(
                        statLabel,
                        labelWidth,
                        statValue.ToString("N2"),
                        baseValue,
                        statPriority.Weight.ToString("N2"),
                        multiplierWidth,
                        statScore.ToString("N2"),
                        finalValue);
                    score += statScore;
                }

                GUI.color = Color.white;
            }

            // end upper group
            EndScrollView();

            // begin lower group
            FlexibleSpace();
            Space(6f);
            Label(string.Empty, this.whiteLine, Height(1));
            Space(6f);
            this.DrawLine(
                string.Empty,
                labelWidth,
                "Modifier",
                baseValue,
                string.Empty,
                multiplierWidth,
                "Subtotal",
                finalValue);

            this.DrawLine(
                "BasicStatusOfApparel".Translate(),
                labelWidth,
                "1.00",
                baseValue,
                "+",
                multiplierWidth,
                score.ToString("N2"),
                finalValue);

            float special = this.apparel.GetSpecialApparelScoreOffset();
            if (special != 0f)
            {
                score += special;

                this.DrawLine(
                    "OutfitterSpecialScore".Translate(),
                    labelWidth,
                    special.ToString("N2"),
                    baseValue,
                    "+",
                    multiplierWidth,
                    score.ToString("N2"),
                    finalValue);
            }

            float armor = ApparelStatCache.ApparelScoreRaw_ProtectionBaseStat(this.apparel) * 0.1f;

            score += armor;

            this.DrawLine(
                "OutfitterArmor".Translate(),
                labelWidth,
                armor.ToString("N2"),
                baseValue,
                "+",
                multiplierWidth,
                score.ToString("N2"),
                finalValue);

            if (this.apparel.def.useHitPoints)
            {
                // durability on 0-1 scale
                float x = this.apparel.HitPoints / (float)this.apparel.MaxHitPoints;
                score = score * 0.25f + score * 0.75f * ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);

                this.DrawLine(
                    "OutfitterHitPoints".Translate(),
                    labelWidth,
                    x.ToString("N2"),
                    baseValue,
                    "weighted",
                    multiplierWidth,
                    score.ToString("N2"),
                    finalValue);

                GUI.color = Color.white;
            }

            if (this.apparel.WornByCorpse && (this.pawn == null
                                              || ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.DeadMansApparel)))
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
                    baseValue,
                    "weighted",
                    multiplierWidth,
                    score.ToString("N2"),
                    finalValue);
            }

            float mod = 1f;
            if (false)
            {
                if (this.apparel.TryGetQuality(out QualityCategory cat))
                {
                    switch (cat)
                    {
                        case QualityCategory.Awful:
                            mod = 0.7f;
                            break;

                        case QualityCategory.Shoddy:
                            mod = 0.8f;
                            break;

                        case QualityCategory.Poor:
                            mod = 0.9f;
                            break;

                        case QualityCategory.Normal:
                            mod = 1.0f;
                            break;

                        case QualityCategory.Good:
                            mod = 1.05f;
                            break;

                        case QualityCategory.Superior:
                            mod = 1.1f;
                            break;

                        case QualityCategory.Excellent:
                            mod = 1.15f;
                            break;

                        case QualityCategory.Masterwork:
                            mod = 1.2f;
                            break;

                        case QualityCategory.Legendary:
                            mod = 1.25f;
                            break;

                        default: throw new ArgumentOutOfRangeException();
                    }
                    score *= mod;

                    this.DrawLine(
                        "OutfitterQuality".Translate(),
                        labelWidth,
                        mod.ToString("N2"),
                        baseValue,
                        "*",
                        multiplierWidth,
                        score.ToString("N2"),
                        finalValue);
                }
            }

            if (this.apparel.Stuff == ThingDefOf.Human.race.leatherDef)
            {
                if (this.pawn == null || ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.HumanLeatherApparelSad))
                {
                    score -= 0.5f;
                    if (score > 0f)
                    {
                        score *= 0.1f;
                    }
                }

                if (this.pawn != null && ThoughtUtility.CanGetThought(this.pawn, ThoughtDefOf.HumanLeatherApparelHappy))
                {
                    score *= 2f;
                }

                this.DrawLine(
                    "OutfitterHumanLeather".Translate(),
                    labelWidth,
                    "modified",
                    baseValue,
                    "weighted",
                    multiplierWidth,
                    score.ToString("N2"),
                    finalValue);
            }

            float temperature = conf.ApparelScoreRaw_Temperature(this.apparel);

            if (Math.Abs(temperature - 1f) > 0)
            {
                score *= temperature;

                this.DrawLine(
                    "OutfitterTemperature".Translate(),
                    labelWidth,
                    conf.ApparelScoreRaw_Temperature(this.apparel).ToString("N2"),
                    baseValue,
                    "*",
                    multiplierWidth,
                    score.ToString("N2"),
                    finalValue);
            }

            this.DrawLine(
                "OutfitterTotal".Translate(),
                labelWidth,
                string.Empty,
                baseValue,
                "=",
                multiplierWidth,
                conf.ApparelScoreRaw(this.apparel, this.pawn).ToString("N2"),
                finalValue);

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

        #endregion Public Methods

#pragma warning disable 649
#pragma warning restore 649
#pragma warning disable 649
#pragma warning restore 649

        #region Private Methods

        private void DrawLine(
            string statDefLabelText,
            float statDefLabelWidth,
            string statDefValueText,
            float statDefValueWidth,
            string multiplierText,
            float multiplierWidth,
            string finalValueText,
            float finalValueWidth,
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
            Label(statDefValueText, Width(statDefValueWidth));
            Label(multiplierText, Width(multiplierWidth));
            Label(finalValueText, Width(finalValueWidth));
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

        #endregion Private Methods
    }
}