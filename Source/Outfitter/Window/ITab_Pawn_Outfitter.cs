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

namespace Outfitter
{
    public class ITab_Pawn_Outfitter : ITab
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;
        private bool CanControl { get { return SelPawn.IsColonistPlayerControlled; } }
        private const float ThingIconSize = 30f;
        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private const float ThingRowHeight = 64f;

        private const float ThingLeftX = 40f;


        public ITab_Pawn_Outfitter()
        {
            size = new Vector2(770f, 550f);
            labelKey = "OutfitterTab";
        }

        private Pawn selPawnForGear
        {
            get
            {
                if (SelPawn != null)
                {
                    return SelPawn;
                }
                Corpse corpse = SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.InnerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + SelThing);
            }
        }

        protected override void FillTab()
        {

            SaveablePawn pawnSave = MapComponent_Outfitter.Get.GetCache(selPawnForGear);

            // Outfit + Status button
            Rect rectStatus = new Rect(10f, 15f, 380f, 30f);
            GUILayout.BeginArea(rectStatus);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            // select outfit

            if (GUILayout.Button(selPawnForGear.outfits.CurrentOutfit.label))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
                {
                    Outfit localOut = current;
                    list.Add(new FloatMenuOption(localOut.label, delegate
                    {
                        selPawnForGear.outfits.CurrentOutfit = localOut;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }


            //edit outfit
            if (GUILayout.Button("OutfitterEditOutfit".Translate() + " " + selPawnForGear.outfits.CurrentOutfit.label + " ..."))
            {
                Find.WindowStack.Add(new Dialog_ManageOutfits(selPawnForGear.outfits.CurrentOutfit));
            }


            ////show outfit
            if (GUILayout.Button(pawnSave.mainJob == SaveablePawn.MainJob.Anything ? "MainJob".Translate() : "PreferedGear".Translate() + " " + pawnSave.mainJob.ToString().Replace("00", " - ").Replace("_", " ")))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (SaveablePawn.MainJob mainJob in Enum.GetValues(typeof(SaveablePawn.MainJob)))
                {
                    options.Add(new FloatMenuOption(mainJob.ToString().Replace("00", " - ").Replace("_", " "), delegate
                    {
                        pawnSave.mainJob = mainJob;
                        pawnSave.forceStatUpdate = true;

                        selPawnForGear.mindState.Notify_OutfitChanged();
                        if ((selPawnForGear.jobs.curJob != null) && selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                        {
                            selPawnForGear.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }));
                }
                FloatMenu window = new FloatMenu(options, "MainJob".Translate());

                Find.WindowStack.Add(window);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.EndArea();


            // Status checkboxes

            Rect rectCheckboxes = new Rect(10f, rectStatus.yMax, rectStatus.width, 60f);
            GUILayout.BeginArea(rectCheckboxes);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            DrawCheckBoxArea("AddWorkStats".Translate(), ref pawnSave.AddWorkStats);
            DrawCheckBoxArea("AddIndividualStats".Translate(), ref pawnSave.AddIndividualStats);
            // if (!peacefulPawn)
            //     DrawCheckBoxArea("AutoEquipWeapon".Translate(), ref pawnSave.AutoEquipWeapon);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(5f);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();


            // if (GUILayout.Button("ApparelStats".Translate()))
            //     isApparel = true;
            //
            // if (!peacefulPawn)
            //     if (GUILayout.Button("ApparelStats".Translate()))
            //         isApparel = false;

            //update outfit
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OutfitterUpdateOutfit".Translate()))
            {
                var parms = new JobIssueParams();
                selPawnForGear.mindState.nextApparelOptimizeTick = -5000;
                new Outfitter_JobGiver_OptimizeApparel().TryIssueJobPackage(selPawnForGear, parms);
            }


            GUILayout.EndHorizontal();
            GUILayout.EndVertical();


            GUILayout.EndArea();


            // main canvas
            Rect canvas = new Rect(0f, 75f, 432, size.y - 75f).ContractedBy(20f);
            GUI.BeginGroup(canvas);
            Vector2 cur = Vector2.zero;

            DrawApparelStats(pawnSave, cur, canvas);
        }

        private void DrawApparelStats(SaveablePawn pawnSave, Vector2 cur, Rect canvas)
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
            cur.y += 10f;
            // temperature slider
            //    SaveablePawn pawnStatCache = MapComponent_Outfitter.Get.GetCache(SelPawn);
            ApparelStatCache pawnStatCache = selPawnForGear.GetApparelStatCache();
            FloatRange targetTemps = pawnStatCache.TargetTemperatures;
            FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;
            Rect sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);
            Rect tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + 10f, 16f, 16f);
            cur.y += 40f; // includes padding 

            // current temperature settings
            GUI.color = pawnSave.TargetTemperaturesOverride ? Color.white : Color.grey;
            Widgets_FloatRange.FloatRange(sliderRect, 123123123, ref targetTemps, minMaxTemps, ToStringStyle.Temperature);
            GUI.color = Color.white;

            if (Math.Abs(targetTemps.min - pawnStatCache.TargetTemperatures.min) > 1e-4 ||
                 Math.Abs(targetTemps.max - pawnStatCache.TargetTemperatures.max) > 1e-4)
            {
                pawnStatCache.TargetTemperatures = targetTemps;
            }

            if (pawnSave.TargetTemperaturesOverride)
            {
                if (Widgets.ButtonImage(tempResetRect, OutfitterTextures.resetButton))
                {
                    pawnSave.TargetTemperaturesOverride = false;
                    //   var saveablePawn = MapComponent_Outfitter.Get.GetCache(SelPawn);
                    //     saveablePawn.targetTemperaturesOverride = false;
                    pawnStatCache.UpdateTemperatureIfNecessary(true);
                }
                TooltipHandler.TipRegion(tempResetRect, "TemperatureRangeReset".Translate());
            }
            Text.Font = GameFont.Small;
            TryDrawComfyTemperatureRange(ref cur.y, canvas.width);


            // header
            Rect statsHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label(statsHeaderRect, "PreferedStats".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            Rect addStatRect = new Rect(statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + 10f, 16f, 16f);
            if (Widgets.ButtonImage(addStatRect, OutfitterTextures.addButton))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (StatDef def in selPawnForGear.NotYetAssignedStatDefs().OrderBy(i => i.label.ToString()))
                {
                    options.Add(new FloatMenuOption(def.LabelCap, delegate
                    {
                        selPawnForGear.GetApparelStatCache()
                            .StatCache.Insert(0, new ApparelStatCache.StatPriority(def, 0f, StatAssignment.Manual));
                        //pawnStatCache.Stats.Insert(0, new Saveable_Pawn_StatDef(def, 0f, StatAssignment.Manual));
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
            cur.y += 10f;

            // main content in scrolling view
            Rect contentRect = new Rect(cur.x, cur.y, canvas.width, canvas.height - cur.y);
            Rect viewRect = contentRect;
            viewRect.height = selPawnForGear.GetApparelStatCache().StatCache.Count * 30f + 10f;
            if (viewRect.height > contentRect.height)
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView(contentRect, ref _scrollPosition, viewRect);
            GUI.BeginGroup(viewRect);
            cur = Vector2.zero;

            // none label
            if (!selPawnForGear.GetApparelStatCache().StatCache.Any())
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
                foreach (ApparelStatCache.StatPriority stat in selPawnForGear.GetApparelStatCache().StatCache)
                {
                    bool stop_UI;
                    ApparelStatCache.DrawStatRow(ref cur, viewRect.width, stat, selPawnForGear, out stop_UI);
                    if (stop_UI)
                    {
                        // DrawWApparelStatRow can change the StatCache, invalidating the loop. So if it does that, stop looping - we'll redraw on the next tick.
                        break;
                    }
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();

            GUI.EndGroup();

            #region Apparel List 

            // main canvas

            Rect rect = new Rect(432, 20, 338, 530);

            Text.Font = GameFont.Small;
            //     Rect rect2 = rect.ContractedBy(10f);
            Rect calcScore = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.BeginGroup(calcScore);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, calcScore.width, calcScore.height);
            Rect viewRect1 = outRect;
            viewRect1.height = scrollViewHeight;
            if (viewRect1.height > outRect.height)
            {
                viewRect1.width -= 20f;
            }
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect1);
            float num = 0f;

            if (SelPawn.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect1.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawn.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    string bp = "";
                    string layer = "";
                    foreach (ApparelLayer apparelLayer in current2.def.apparel.layers)
                    {
                        foreach (BodyPartGroupDef bodyPartGroupDef in current2.def.apparel.bodyPartGroups)
                        {
                            bp += bodyPartGroupDef.LabelCap + " - ";
                        }
                        layer = apparelLayer.ToString();
                    }
                    Widgets.ListSeparator(ref num, viewRect1.width, bp + layer);
                    DrawThingRowModded(ref num, viewRect1.width, current2);
                }
            }


            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            #endregion
        }

        private static void DrawCheckBoxArea(string name, ref bool stat)
        {
            stat = GUILayout.Toggle(stat, name);
        }

        public override bool IsVisible
        {
            get
            {
                Pawn selectedPawn = SelPawn;

                // thing selected is a pawn
                if (selectedPawn == null)
                {
                    Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelDetail), false);
                    //       Find.WindowStack.TryRemove(typeof(Window_Pawn_ApparelList), false);

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

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (selPawnForGear.Dead)
            {
                return;
            }
            Rect rect = new Rect(0f, curY, width, 22f);
            float statValue = selPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float statValue2 = selPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax);
            Widgets.Label(rect, string.Concat("ComfyTemperatureRange".Translate(), ": ", statValue.ToStringTemperature("F0"), " ~ ", statValue2.ToStringTemperature("F0")));
            curY += 22f;
        }

        private void DrawThingRowModded(ref float y, float width, Apparel apparel)
        {

            if (apparel == null)
            {
                DrawThingRowVanilla(ref y, width, apparel);
                return;
            }



            Rect rect = new Rect(0f, y, width, ThingRowHeight);

            if (Mouse.IsOver(rect))
            {
                GUI.color = HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            GUI.color = ThingLabelColor;

            #region Button Clicks

            // LMB doubleclick

            if (Widgets.ButtonInvisible(rect))
            {
                //Left Mouse Button Menu
                if (Event.current.button == 0)
                {
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(SelPawn, apparel));
                }

                // RMB menu
                if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(apparel));
                    }));



                    if (CanControl)
                    {
                        floatOptionList.Add(new FloatMenuOption("OutfitterComparer".Translate(), delegate
                        {
                            Find.WindowStack.Add(new Dialog_PawnApparelComparer(selPawnForGear, apparel));
                        }));

                        Action dropApparel = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDrop(apparel);
                        };
                        Action dropApparelHaul = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDropHaul(apparel);
                        };
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), dropApparel));
                        floatOptionList.Add(new FloatMenuOption("DropThingHaul".Translate(), dropApparelHaul));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, "");
                    Find.WindowStack.Add(window);
                }
            }

            #endregion Button Clicks


            if (apparel.def.DrawMatSingle != null && apparel.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y + 5f, ThingIconSize, ThingIconSize), apparel);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight - Text.LineHeight);
            Rect scoreRect = new Rect(ThingLeftX, textRect.yMax, width - ThingLeftX, Text.LineHeight);

            #region Modded

            ApparelStatCache conf = new ApparelStatCache(SelPawn);
            string text = apparel.LabelCap;
            string text_Score = Math.Round(conf.ApparelScoreRaw(apparel as Apparel, SelPawn), 2).ToString("N2");

            #endregion


            if (apparel is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)apparel))
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
                GUI.color = (HighlightColor);
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
            if (CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, OutfitterTextures.Drop))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    InterfaceDrop(thing);
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
            if (thing is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
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
                Pawn selPawnForGear = SelPawn;
                if (selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out thing);
            }
        }

        private void InterfaceDropHaul(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null)
            {
                Pawn selPawnForGear = SelPawn;
                if (selPawnForGear.jobs.IsCurrentJobPlayerInterruptible())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    job.haulDroppedApparel = true;
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipmentListForReading.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.innerContainer.TryDrop(t, ThingPlaceMode.Near, out thing);
            }
        }
    }
}
