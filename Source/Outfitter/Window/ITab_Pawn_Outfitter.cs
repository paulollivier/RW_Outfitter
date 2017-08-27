namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Outfitter.Textures;
    using Outfitter.Window;
    using RimWorld;
    using UnityEngine;
    using Verse;
    using Verse.AI;
    using Verse.Sound;

    public class ITab_Pawn_Outfitter : ITab
    {

        #region Private Fields

        private const float ButtonHeight = 30f;

        private const float Margin = 10f;

        private const float ThingIconSize = 30f;

        private const float ThingLeftX = 40f;

        private const float ThingRowHeight = 64f;

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private Vector2 scrollPosition = Vector2.zero;

        private Vector2 scrollPosition1 = Vector2.zero;

        private float scrollViewHeight;

        private float scrollViewHeight1;

        #endregion Private Fields

        #region Public Constructors

        public ITab_Pawn_Outfitter()
        {
            this.size = new Vector2(770f, 550f);
            this.labelKey = "OutfitterTab";
        }

        #endregion Public Constructors

        #region Public Properties

        public override bool IsVisible
        {
            get
            {
                Pawn selectedPawn = this.SelPawn;

                // thing selected is a pawn
                if (selectedPawn == null)
                {
                    Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelDetail), false);

                    // Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelList), false);
                    return false;
                }

                // of this colony
                if (selectedPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (selectedPawn.apparel == null)
                {
                    return false;
                }

                return true;
            }
        }

        #endregion Public Properties

        #region Private Properties

        private bool CanControl => this.SelPawn.IsColonistPlayerControlled;

        private Pawn SelPawnForGear
        {
            get
            {
                if (this.SelPawn != null)
                {
                    return this.SelPawn;
                }

                Corpse corpse = this.SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }

                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + this.SelThing);
            }
        }

        #endregion Private Properties

        #region Protected Methods

        protected override void FillTab()
        {
            SaveablePawn pawnSave = GameComponent_Outfitter.GetCache(this.SelPawnForGear);

            // Outfit + Status button
            Rect rectStatus = new Rect(20f, 15f, 380f, ButtonHeight);

            Rect outfitRect = new Rect(rectStatus.x, rectStatus.y, 392f / 3 - Margin, ButtonHeight);

            Rect outfitEditRect = new Rect(outfitRect.xMax + Margin, outfitRect.y, outfitRect.width, ButtonHeight);

            Rect outfitJobRect = new Rect(outfitEditRect.xMax + Margin, outfitRect.y, outfitRect.width, ButtonHeight);

            // select outfit
            if (Widgets.ButtonText(outfitRect, this.SelPawnForGear.outfits.CurrentOutfit.label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
                {
                    Outfit localOut = current;
                    options.Add(
                        new FloatMenuOption(
                            localOut.label,
                            delegate { this.SelPawnForGear.outfits.CurrentOutfit = localOut; }));
                }

                FloatMenu window = new FloatMenu(options, "SelectOutfit".Translate());

                Find.WindowStack.Add(window);
            }

            // edit outfit
            if (Widgets.ButtonText(
                outfitEditRect,
                "OutfitterEditOutfit".Translate() + " " + this.SelPawnForGear.outfits.CurrentOutfit.label + " ..."))
            {
                Find.WindowStack.Add(new Dialog_ManageOutfits(this.SelPawnForGear.outfits.CurrentOutfit));
            }

            // job outfit
            if (Widgets.ButtonText(
                outfitJobRect,
                pawnSave.mainJob == SaveablePawn.MainJob.Anything
                    ? "MainJob".Translate()
                    : "PreferedGear".Translate() + " " + pawnSave.mainJob.ToString().Replace("00", " - ")
                          .Replace("_", " ")))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (SaveablePawn.MainJob mainJob in Enum.GetValues(typeof(SaveablePawn.MainJob)))
                {
                    options.Add(
                        new FloatMenuOption(
                            mainJob.ToString().Replace("00", " - ").Replace("_", " "),
                            delegate
                                {
                                    pawnSave.mainJob = mainJob;
                                    pawnSave.forceStatUpdate = true;

                                    this.SelPawnForGear.mindState.Notify_OutfitChanged();
                                    if (this.SelPawnForGear.jobs.curJob != null
                                        && this.SelPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                                    {
                                        this.SelPawnForGear.jobs.EndCurrentJob(JobCondition.InterruptForced);
                                    }
                                }));
                }

                FloatMenu window = new FloatMenu(options, "MainJob".Translate());

                Find.WindowStack.Add(window);
            }

            // Status checkboxes
            Rect rectCheckboxes = new Rect(rectStatus.x, rectStatus.yMax + Margin, rectStatus.width, 48f);
            Rect check1 = new Rect(rectCheckboxes.x, rectCheckboxes.y, rectCheckboxes.width, 24f);
            Rect check2 = new Rect(rectCheckboxes.x, check1.yMax, rectCheckboxes.width, 24f);
            DrawCheckBoxArea(check1, "AddWorkStats".Translate(), ref pawnSave.AddWorkStats);
            DrawCheckBoxArea(check2, "AddIndividualStats".Translate(), ref pawnSave.AddIndividualStats);

            // main canvas
            Rect canvas = new Rect(20f, rectCheckboxes.yMax, 392f, this.size.y - rectCheckboxes.yMax - 20f);
            GUI.BeginGroup(canvas);
            Vector2 cur = Vector2.zero;

            this.DrawTemperatureStats(pawnSave, ref cur, canvas);
            cur.y += Margin;
            this.DrawApparelStats(cur, canvas);

            GUI.EndGroup();

            this.DrawApparelList();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        #endregion Protected Methods

        #region Private Methods

        private static void DrawCheckBoxArea(Rect rect, string name, ref bool stat)
        {
            Widgets.CheckboxLabeled(rect, name, ref stat);
        }

        private void DrawApparelList()
        {
            // main canvas
            Rect rect = new Rect(432, 20, 318, 530);

            Text.Font = GameFont.Small;

            // Rect rect2 = rect.ContractedBy(10f);
            Rect calcScore = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.BeginGroup(calcScore);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, calcScore.width, calcScore.height);
            Rect viewRect1 = outRect;
            viewRect1.height = this.scrollViewHeight1;
            if (viewRect1.height > outRect.height)
            {
                viewRect1.width -= 20f;
            }

            Widgets.BeginScrollView(outRect, ref this.scrollPosition1, viewRect1);
            float num = 0f;

            if (this.SelPawn.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect1.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in this.SelPawn.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    string bp = string.Empty;
                    string layer = string.Empty;
                    foreach (ApparelLayer apparelLayer in current2.def.apparel.layers)
                    {
                        foreach (BodyPartGroupDef bodyPartGroupDef in current2.def.apparel.bodyPartGroups)
                        {
                            bp += bodyPartGroupDef.LabelCap + " - ";
                        }

                        layer = apparelLayer.ToString();
                    }

                    Widgets.ListSeparator(ref num, viewRect1.width, bp + layer);
                    this.DrawThingRowModded(ref num, viewRect1.width, current2);
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight1 = num + 30f;
            }

            Widgets.EndScrollView();

            GUI.EndGroup();
        }

        private void DrawApparelStats(Vector2 cur, Rect canvas)
        {
            // header
            Rect statsHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label(statsHeaderRect, "PreferedStats".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            Rect addStatRect = new Rect(statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + Margin, 16f, 16f);
            if (Widgets.ButtonImage(addStatRect, OutfitterTextures.AddButton))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (StatDef def in this.SelPawnForGear.NotYetAssignedStatDefs().OrderBy(i => i.label.ToString()))
                {
                    options.Add(
                        new FloatMenuOption(
                            def.LabelCap,
                            delegate
                                {
                                    this.SelPawnForGear.GetApparelStatCache().StatCache.Insert(
                                        0,
                                        new ApparelStatCache.StatPriority(def, 0f, StatAssignment.Manual));

                                    // pawnStatCache.Stats.Insert(0, new Saveable_Pawn_StatDef(def, 0f, StatAssignment.Manual));
                                }));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            TooltipHandler.TipRegion(addStatRect, "StatPriorityAdd".Translate());

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += Margin;

            // main content in scrolling view
            Rect contentRect = new Rect(cur.x, cur.y, canvas.width, canvas.height - cur.y);
            Rect viewRect = contentRect;
            viewRect.height = this.scrollViewHeight;
            if (viewRect.height > contentRect.height)
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView(contentRect, ref this.scrollPosition, viewRect);

            GUI.BeginGroup(viewRect);
            cur = Vector2.zero;

            // none label
            if (!this.SelPawnForGear.GetApparelStatCache().StatCache.Any())
            {
                Rect noneLabel = new Rect(cur.x, cur.y, viewRect.width, 30f);
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(noneLabel, "None".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                cur.y += 30f;
            }
            else
            {
                // legend kind of thingy.
                Rect legendRect = new Rect(cur.x + (viewRect.width - 24) / 2, cur.y, (viewRect.width - 24) / 2, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(legendRect, "-2.5");
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(legendRect, "2.5");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                cur.y += 15f;

                // stat weight sliders
                foreach (ApparelStatCache.StatPriority stat in this.SelPawnForGear.GetApparelStatCache().StatCache)
                {
                    ApparelStatCache.DrawStatRow(ref cur, viewRect.width, stat, this.SelPawnForGear, out bool stop_UI);
                    if (stop_UI)
                    {
                        // DrawWApparelStatRow can change the StatCache, invalidating the loop. So if it does that, stop looping - we'll redraw on the next tick.
                        break;
                    }
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight = cur.y + 10f;
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        private void DrawTemperatureStats(SaveablePawn pawnSave, ref Vector2 cur, Rect canvas)
        {
            // header
            Rect tempHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(tempHeaderRect, "PreferedTemperature".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += Margin;

            // temperature slider
            // SaveablePawn pawnStatCache = MapComponent_Outfitter.Get.GetCache(SelPawn);
            ApparelStatCache pawnStatCache = this.SelPawnForGear.GetApparelStatCache();
            FloatRange targetTemps = pawnStatCache.TargetTemperatures;
            FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;
            Rect sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);
            Rect tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + Margin, 16f, 16f);
            cur.y += 40f; // includes padding

            // current temperature settings
            GUI.color = pawnSave.TargetTemperaturesOverride ? Color.white : Color.grey;
            Widgets_FloatRange.FloatRange(
                sliderRect,
                123123123,
                ref targetTemps,
                minMaxTemps,
                ToStringStyle.Temperature);
            GUI.color = Color.white;

            if (Math.Abs(targetTemps.min - pawnStatCache.TargetTemperatures.min) > 1e-4
                || Math.Abs(targetTemps.max - pawnStatCache.TargetTemperatures.max) > 1e-4)
            {
                pawnStatCache.TargetTemperatures = targetTemps;
            }

            if (pawnSave.TargetTemperaturesOverride)
            {
                if (Widgets.ButtonImage(tempResetRect, OutfitterTextures.ResetButton))
                {
                    pawnSave.TargetTemperaturesOverride = false;

                    // var saveablePawn = MapComponent_Outfitter.Get.GetCache(SelPawn);
                    // saveablePawn.targetTemperaturesOverride = false;
                    pawnStatCache.UpdateTemperatureIfNecessary(true);
                }

                TooltipHandler.TipRegion(tempResetRect, "TemperatureRangeReset".Translate());
            }

            Text.Font = GameFont.Small;
            this.TryDrawComfyTemperatureRange(ref cur.y, canvas.width);
        }

        private void DrawThingRowModded(ref float y, float width, Apparel apparel)
        {
            if (apparel == null)
            {
                this.DrawThingRowVanilla(ref y, width, apparel);
                return;
            }

            Rect rect = new Rect(0f, y, width, ThingRowHeight);

            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            GUI.color = ThingLabelColor;

            // LMB doubleclick
            if (Widgets.ButtonInvisible(rect))
            {
                // Left Mouse Button Menu
                if (Event.current.button == 0)
                {
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(this.SelPawn, apparel));
                }

                // RMB menu
                if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList =
                        new List<FloatMenuOption>
                            {
                                new FloatMenuOption(
                                    "ThingInfo".Translate(),
                                    delegate
                                        {
                                            Find.WindowStack.Add(new Dialog_InfoCard(apparel));
                                        })
                            };

                    if (this.CanControl)
                    {
                        floatOptionList.Add(
                            new FloatMenuOption(
                                "OutfitterComparer".Translate(),
                                delegate
                                    {
                                        Find.WindowStack.Add(
                                            new Dialog_PawnApparelComparer(this.SelPawnForGear, apparel));
                                    }));

                        Action dropApparel = delegate
                            {
                                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                                this.InterfaceDrop(apparel);
                            };
                        Action dropApparelHaul = delegate
                            {
                                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                                this.InterfaceDropHaul(apparel);
                            };
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), dropApparel));
                        floatOptionList.Add(new FloatMenuOption("DropThingHaul".Translate(), dropApparelHaul));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, string.Empty);
                    Find.WindowStack.Add(window);
                }
            }

            if (apparel.def.DrawMatSingle != null && apparel.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y + 5f, ThingIconSize, ThingIconSize), apparel);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight - Text.LineHeight);
            Rect scoreRect = new Rect(ThingLeftX, textRect.yMax, width - ThingLeftX, Text.LineHeight);

            ApparelStatCache conf = new ApparelStatCache(this.SelPawn);
            string text = apparel.LabelCap;
            string text_Score = Math.Round(conf.ApparelScoreRaw(apparel, this.SelPawn), 2).ToString("N2");

            if (apparel is Apparel && this.SelPawn.outfits != null
                && this.SelPawn.outfits.forcedHandler.IsForced(apparel))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
                Widgets.Label(textRect, text);
            }
            else
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                if (apparel.def.useHitPoints)
                {
                    float x = apparel.HitPoints / (float)apparel.MaxHitPoints;
                    if (x < 0.5f)
                    {
                        GUI.color = Color.yellow;
                    }

                    if (x < 0.2f)
                    {
                        GUI.color = Color.red;
                    }
                }

                Widgets.Label(textRect, text);
                GUI.color = Color.white;
                Widgets.Label(scoreRect, text_Score);
            }

            y += ThingRowHeight;
        }

        private void DrawThingRowVanilla(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            GUI.color = ThingLabelColor;
            Rect rect2a = new Rect(rect.width - 24f, y, 24f, 24f);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            TooltipHandler.TipRegion(rect2a, "DefInfoTip".Translate());
            if (Widgets.ButtonImage(rect2a, OutfitterTextures.Info))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(thing));
            }

            if (this.CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, OutfitterTextures.Drop))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    this.InterfaceDrop(thing);
                }

                rect.width -= 24f;
            }

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect3 = new Rect(ThingLeftX, y, width - ThingLeftX, 28f);
            string text = thing.LabelCap;
            if (thing is Apparel && this.SelPawn.outfits != null
                && this.SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }

            Widgets.Label(rect3, text);
            y += ThingRowHeight;
        }

        private void InterfaceDrop(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null)
            {
                Pawn selPawnForGear = this.SelPawn;
                if (selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel) { playerForced = true };
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null
                     && this.SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                this.SelPawn.equipment.TryDropEquipment(
                    thingWithComps,
                    out ThingWithComps thingWithComps2,
                    this.SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                this.SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out Thing thing);
            }
        }

        private void InterfaceDropHaul(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null)
            {
                Pawn selPawnForGear = this.SelPawn;
                if (selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    Job job =
                        new Job(JobDefOf.RemoveApparel, apparel) { playerForced = true, haulDroppedApparel = true };
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null
                     && this.SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                this.SelPawn.equipment.TryDropEquipment(
                    thingWithComps,
                    out ThingWithComps thingWithComps2,
                    this.SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                this.SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out Thing thing);
            }
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (this.SelPawnForGear.Dead)
            {
                return;
            }

            Rect rect = new Rect(0f, curY, width, 22f);
            float statValue = this.SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float statValue2 = this.SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax);
            Widgets.Label(
                rect,
                string.Concat(
                    "ComfyTemperatureRange".Translate(),
                    ": ",
                    statValue.ToStringTemperature("F0"),
                    " ~ ",
                    statValue2.ToStringTemperature("F0")));
            curY += 22f;
        }

        #endregion Private Methods

    }
}