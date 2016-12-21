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
            #region Base Tab

            SaveablePawn pawnSave = MapComponent_Outfitter.Get.GetCache(selPawnForGear);

            // Outfit + Status button
            Rect rectStatus = new Rect(10f, 15f, 120f, 30f);

            // select outfit

            if (Widgets.ButtonText(rectStatus, selPawnForGear.outfits.CurrentOutfit.label))
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
            rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);

            if (Widgets.ButtonText(rectStatus, "OutfitterEditOutfit".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ManageOutfits(selPawnForGear.outfits.CurrentOutfit));
            }

            ////show outfit
            //rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);
            //
            //if (Widgets.ButtonText(rectStatus, "OutfitShow".Translate(), true, false))
            //{
            //    Find.WindowStack.Add(new Window_Pawn_ApparelList());
            //}

            rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);
            if (Widgets.ButtonText(rectStatus, pawnSave.mainJob == SaveablePawn.MainJob.Anything ? "MainJob".Translate() : pawnSave.mainJob.ToString().Replace("00", " - ").Replace("_", " ")))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (SaveablePawn.MainJob mainJob in Enum.GetValues(typeof(SaveablePawn.MainJob)))
                {
                    options.Add(new FloatMenuOption(mainJob.ToString().Replace("00", " - ").Replace("_", " "), delegate
                    {
                        pawnSave.mainJob = mainJob;
                        pawnSave.forceStatUpdate = true;

                        selPawnForGear.mindState.Notify_OutfitChanged();
                        if ((selPawnForGear.jobs.curJob != null) && selPawnForGear.jobs.CanTakeOrderedJob())
                        {
                            selPawnForGear.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }));
                }
                FloatMenu window = new FloatMenu(options, "MainJob".Translate());

                Find.WindowStack.Add(window);
            }




            // Status checkboxes
            Rect rectCheckboxes = new Rect(10f, rectStatus.yMax + 15f, 130f, rectStatus.height);
            Text.Font = GameFont.Small;
            pawnSave.AddWorkStats = GUI.Toggle(new Rect(10f, rectCheckboxes.y, 120f, rectCheckboxes.height), pawnSave.AddWorkStats, "AddWorkStats".Translate());
            pawnSave.AddIndividualStats = GUI.Toggle(new Rect(140f, rectCheckboxes.y, rectCheckboxes.width + 10f, rectCheckboxes.height),
                pawnSave.AddIndividualStats, "AddIndividualStats".Translate());


            // main canvas
            Rect canvas = new Rect(0f, 60f, 432, size.y - 60f).ContractedBy(20f);
            GUI.BeginGroup(canvas);
            Vector2 cur = Vector2.zero;

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
            Widgets.Label(statsHeaderRect, "PreferredStats".Translate());
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
                      selPawnForGear.GetApparelStatCache().StatCache.Insert(0, new ApparelStatCache.StatPriority(def, 0f, StatAssignment.Manual));
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
                        // DrawStatRow can change the StatCache, invalidating the loop. So if it does that, stop looping - we'll redraw on the next tick.
                        break;
                    }
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();

            GUI.EndGroup();

            #endregion

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
        /*
        private Job WearApparel()
        {
                Outfit currentOutfit = selPawnForGear.outfits.CurrentOutfit;
                List<Apparel> wornApparel = selPawnForGear.apparel.WornApparel;
                for (int i = wornApparel.Count - 1; i >= 0; i--)
                {
                    if (!currentOutfit.filter.Allows(wornApparel[i]) &&
                        selPawnForGear.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                    {
                        return new Job(JobDefOf.RemoveApparel, wornApparel[i])
                        {
                            haulDroppedApparel = true
                        };
                    }
                }
                Thing thing = null;
                float num = 0f;
                List<Thing> list = Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel);
                if (list.Count == 0)
                {
                    return null;
                }
                foreach (Thing apparelthing in list)
                {
                    Apparel apparel = (Apparel) apparelthing;
                    if (currentOutfit.filter.Allows(apparel))
                    {
                        if (Find.SlotGroupManager.SlotGroupAt(apparel.Position) != null)
                        {
                            if (!apparel.IsForbidden(selPawnForGear))
                            {
                                float num2 = ApparelStatsHelper.ApparelScoreGain(selPawnForGear, apparel);

                                if (num2 >= 0.09f && num2 >= num)
                                {
                                    if (ApparelUtility.HasPartsToWear(selPawnForGear, apparel.def))
                                    {
                                        if (selPawnForGear.CanReserveAndReach(apparel, PathEndMode.OnCell,
                                            selPawnForGear.NormalMaxDanger(), 1))
                                        {
                                            thing = apparel;
                                            num = num2;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (thing == null)
                {
                    return null;
                }
                return new Job(JobDefOf.Wear, thing);
            
        }
*/


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

        private void DrawThingRowModded(ref float y, float width, Thing thing)
        {
            Apparel apparel = thing as Apparel;

            if (apparel == null)
            {
                DrawThingRowVanilla(ref y, width, thing);
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
                else if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(thing));
                    }));

                    floatOptionList.Add(new FloatMenuOption("OutfitterComparer".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_PawnApparelComparer(SelPawn, apparel));
                    }));

                    if (CanControl)
                    {
                        Action dropApparel = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDrop(thing);
                        };
                        Action dropApparelHaul = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDropHaul(thing);
                        };
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), dropApparel));
                        floatOptionList.Add(new FloatMenuOption("DropThingHaul".Translate(), dropApparelHaul));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, "");
                    Find.WindowStack.Add(window);
                }
            }

            #endregion Button Clicks


            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y + 5f, ThingIconSize, ThingIconSize), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight - Text.LineHeight);
            Rect scoreRect = new Rect(ThingLeftX, textRect.yMax, width - ThingLeftX, Text.LineHeight);
            #region Modded
            ApparelStatCache conf = new ApparelStatCache(SelPawn);
            string text = thing.LabelCap;
            string text_Score = Math.Round(conf.ApparelScoreRaw(apparel, SelPawn), 2).ToString("N2");

            #endregion


            if (thing is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
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
                if (selPawnForGear.jobs.CanTakeOrderedJob())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipment.Contains(thingWithComps))
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
                if (selPawnForGear.jobs.CanTakeOrderedJob())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    job.haulDroppedApparel = true;
                    selPawnForGear.jobs.TryTakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipment.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.innerContainer.TryDrop(t,  ThingPlaceMode.Near, out thing);
            }
        }
    }
}
