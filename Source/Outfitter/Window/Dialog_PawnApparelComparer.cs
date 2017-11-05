namespace Outfitter.Window
{
    using JetBrains.Annotations;
    using RimWorld;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Verse;

    public class Dialog_PawnApparelComparer : Window
    {
        [NotNull]
        private readonly Apparel apparel;

        [NotNull]
        private readonly Pawn pawn;

        private Vector2 scrollPosition;

        public Dialog_PawnApparelComparer(Pawn p, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;

            this.pawn = p;
            this.apparel = apparel;
        }

        public override Vector2 InitialSize => new Vector2(500f, 700f);

        public override void DoWindowContents(Rect windowRect)
        {
            ApparelStatCache apparelStatCache = new ApparelStatCache(GameComponent_Outfitter.GetCache(this.pawn));
            List<Apparel> allApparels = new List<Apparel>(
                this.pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>());
            foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists.Where(x => x.Map == this.pawn.Map))
            {
                foreach (Apparel pawnApparel in pawn.apparel.WornApparel)
                {
                    if (pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                    {
                        allApparels.Add(pawnApparel);
                    }
                }
            }

            allApparels = allApparels.Where(
                i => !ApparelUtility.CanWearTogether(this.apparel.def, i.def, this.pawn.RaceProps.body)).ToList();

            Rect groupRect = windowRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);

            float apparelScoreWidth = 100f;
            float apparelGainWidth = 100f;
            float apparelLabelWidth = (groupRect.width - apparelScoreWidth - apparelGainWidth) / 3 - 8f - 8f;
            float apparelEquippedWidth = apparelLabelWidth;
            float apparelOwnerWidth = apparelLabelWidth;

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, 28f);

            this.DrawLine(
                ref itemRect,
                null,
                "Apparel",
                apparelLabelWidth,
                null,
                "Equiped",
                apparelEquippedWidth,
                null,
                "Target",
                apparelOwnerWidth,
                "Score",
                apparelScoreWidth,
                "Gain",
                apparelGainWidth);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f;

            Rect viewRect = new Rect(
                groupRect.xMin,
                groupRect.yMin,
                groupRect.width - 16f,
                allApparels.Count * 28f + 16f);
            if (viewRect.height < groupRect.height)
            {
                groupRect.height = viewRect.height;
            }

            Rect listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref this.scrollPosition, viewRect);

            allApparels = allApparels.OrderByDescending(
                i =>
                    {
                        DIALOG_CalculateApparelScoreGain(this.pawn, i, out float g);
                        return g;
                    }).ToList();

            foreach (Apparel currentAppel in allApparels)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, 28f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                Pawn equiped = null;
                Pawn target = null;

                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    foreach (Apparel a in pawn.apparel.WornApparel)
                    {
                        if (a == currentAppel)
                        {
                            equiped = pawn;
                            break;
                        }
                    }

                    // foreach (Apparel a in mapComponent.GetCache(pawn).targetApparel)
                    // if (a == currentAppel)
                    // {
                    // target = pawn;
                    // break;
                    // }
                    if (equiped != null && target != null)
                    {
                        break;
                    }
                }

                DIALOG_CalculateApparelScoreGain(this.pawn, currentAppel, out float gain);
                string gainString = this.pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(currentAppel)
                                        ? gain.ToString("N5")
                                        : "No Allow";

                this.DrawLine(
                    ref itemRect,
                    currentAppel,
                    currentAppel.LabelCap,
                    apparelLabelWidth,
                    equiped,
                    equiped?.LabelCap,
                    apparelEquippedWidth,
                    target,
                    target?.LabelCap,
                    apparelOwnerWidth,
                    apparelStatCache.ApparelScoreRaw(currentAppel, this.pawn).ToString("N5"),
                    apparelScoreWidth,
                    gainString,
                    apparelGainWidth);

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }
        private List<Apparel> _calculatedApparelItems;

        private List<float> _calculatedApparelScore;

        public void DIALOG_CalculateApparelScoreGain(Pawn pawn, Apparel apparel, out float gain)
        {
            if (this._calculatedApparelItems == null)
            {
                this.DIALOG_InitializeCalculatedApparelScoresFromWornApparel();
            }

            gain = pawn.ApparelScoreGain(apparel);
        }

        private void DIALOG_InitializeCalculatedApparelScoresFromWornApparel()
        {
            var conf = this.pawn.GetApparelStatCache();
            this._calculatedApparelItems = new List<Apparel>();
            this._calculatedApparelScore = new List<float>();
            foreach (Apparel apparel in this.pawn.apparel.WornApparel)
            {
                this._calculatedApparelItems.Add(apparel);
                this._calculatedApparelScore.Add(conf.ApparelScoreRaw(apparel, this.pawn));
            }
        }

        private void DrawLine(
            ref Rect itemRect,
            [CanBeNull] Apparel apparelThing,
            string apparelText,
            float textureWidth,
            Pawn apparelEquippedThing,
            string apparelEquipedText,
            float apparelEquippedWidth,
            Pawn apparelOwnerThing,
            string apparelOwnerText,
            float apparelOwnerWidth,
            string apparelScoreText,
            float apparelScoreWidth,
            string apparelGainText,
            float apparelGainWidth)
        {
            Rect fieldRect;
            if (apparelThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelText);
                }

                if (apparelThing.def.DrawMatSingle != null && apparelThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    if (apparelEquippedThing != null)
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(apparelEquippedThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelEquippedThing.Spawned)
                        {
                            Find.Selector.Select(apparelEquippedThing);
                        }
                    }
                    else
                    {
                        Find.CameraDriver.JumpToVisibleMapLoc(apparelThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelThing.Spawned)
                        {
                            Find.Selector.Select(apparelThing);
                        }
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, textureWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += textureWidth;

            if (apparelEquippedThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelEquipedText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelEquipedText);
                }

                if (apparelEquippedThing.def.DrawMatSingle != null
                    && apparelEquippedThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelEquippedThing);
                }

                if (Widgets.ButtonInvisible(fieldRect))
                {
                    this.Close();
                    Find.MainTabsRoot.EscapeCurrentTab();
                    Find.CameraDriver.JumpToVisibleMapLoc(apparelEquippedThing.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (apparelEquippedThing.Spawned)
                    {
                        Find.Selector.Select(apparelEquippedThing);
                    }

                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelEquipedText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelEquippedWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }

            itemRect.xMin += apparelEquippedWidth;

            if (apparelOwnerThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    TooltipHandler.TipRegion(fieldRect, apparelOwnerText);
                }

                if (apparelOwnerThing.def.DrawMatSingle != null
                    && apparelOwnerThing.def.DrawMatSingle.mainTexture != null)
                {
                    Widgets.ThingIcon(fieldRect, apparelOwnerThing);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelOwnerWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelOwnerText);
                }
            }

            itemRect.xMin += apparelOwnerWidth;

            fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelScoreWidth, itemRect.height);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fieldRect, apparelScoreText);
            if (apparelThing != null)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(fieldRect))
                {
                    Find.WindowStack.Add(new Window_Pawn_ApparelDetail(this.pawn, apparelThing));
                    return;
                }
            }

            itemRect.xMin += apparelScoreWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, apparelGainWidth, itemRect.height), apparelGainText);
            itemRect.xMin += apparelGainWidth;
        }
    }
}