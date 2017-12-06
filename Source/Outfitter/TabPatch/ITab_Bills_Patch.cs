namespace Outfitter.TabPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using KillfaceTools.FMO;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class ITab_Bills_Patch
    {
        private const string Separator = "   ";

        private const string newLine = "\n-------------------------------";

        private static float viewHeight = 1000f;

        private static Vector2 scrollPosition;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        private static Bill mouseoverBill;

        // RimWorld.ITab_Bills
        public static bool FillTab_Prefix()
        {
            Building_WorkTable selTable = (Building_WorkTable)Find.Selector.SingleSelectedThing;
            if (selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            float x = WinSize.x;
            Vector2 winSize2 = WinSize;
            Rect rect2 = new Rect(0f, 0f, x, winSize2.y).ContractedBy(10f);

            Func<Dictionary<string, List<FloatMenuOption>>> labeledSortingActions = delegate
                {
                    Dictionary<string, List<FloatMenuOption>> dictionary =
                        new Dictionary<string, List<FloatMenuOption>>();

                    // Dictionary<string, List<FloatMenuOption>> dictionary2 = new Dictionary<string, List<FloatMenuOption>>();
                    List<RecipeDef> recipesWithoutPart = selTable.def.AllRecipes
                        .Where(
                            bam => bam.products?.FirstOrDefault()?.thingDef?.apparel?.bodyPartGroups.NullOrEmpty()
                                   ?? true).ToList();
                    List<RecipeDef> recipesWithPart = selTable.def.AllRecipes
                        .Where(
                            bam => !bam.products?.FirstOrDefault()?.thingDef?.apparel?.bodyPartGroups.NullOrEmpty()
                                   ?? false).ToList();
                    recipesWithPart.SortByDescending(blum => blum.label);

                    for (int i = 0; i < recipesWithoutPart.Count; i++)
                    {
                        if (recipesWithoutPart[i].AvailableNow)
                        {
                            RecipeDef recipe = recipesWithoutPart[i];

                            void Action()
                            {
                                bool any = false;
                                foreach (Pawn col in selTable.Map.mapPawns.FreeColonists)
                                {
                                    if (recipe.PawnSatisfiesSkillRequirements(col))
                                    {
                                        any = true;
                                        break;
                                    }
                                }
                                if (!any)
                                {
                                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                                }

                                Bill bill = recipe.MakeNewBill();
                                selTable.billStack.AddBill(bill);
                                if (recipe.conceptLearned != null)
                                {
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                                        recipe.conceptLearned,
                                        KnowledgeAmount.Total);
                                }

                                if (TutorSystem.TutorialMode)
                                {
                                    TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap);
                                }
                            }

                            FloatMenuOption floatMenuOption = new FloatMenuOption(
                                recipe.LabelCap,
                                Action,
                                MenuOptionPriority.Default,
                                null,
                                null,
                                29f,
                                rect => Widgets.InfoCardButton(
                                    (float)(rect.x + 5.0),
                                    (float)(rect.y + (rect.height - 24.0) / 2.0),
                                    recipe));

                            dictionary.Add(recipe.LabelCap, new List<FloatMenuOption>() { floatMenuOption });
                        }
                    }

                    for (int i = 0; i < recipesWithPart.Count; i++)
                    {
                        if (recipesWithPart[i].AvailableNow)
                        {
                            RecipeDef recipe = recipesWithPart[i];

                            ThingCountClass recipeProduct = recipe.products.FirstOrDefault();

                            List<Pawn> colonistsWithThing = new List<Pawn>();
                            if (recipeProduct.thingDef.IsApparel)
                            {
                                colonistsWithThing = selTable.Map.mapPawns.FreeColonistsSpawned
                                    .Where(p => p.apparel.WornApparel.Any(ap => ap.def == recipeProduct.thingDef))
                                    .ToList();
                            }

                            void MouseoverGuiAction()
                            {
                                string tooltip = string.Empty;

                                for (int index = 0; index < recipe.ingredients.Count; index++)
                                {
                                    IngredientCount ingredient = recipe.ingredients[index];
                                    if (index > 0)
                                    {
                                        tooltip += ", ";
                                    }

                                    tooltip += ingredient.Summary;
                                }

                                tooltip += "\n";

                                ThingDef thingDef = recipeProduct.thingDef;
                                for (int index = 0; index < thingDef.apparel.bodyPartGroups.Count; index++)
                                {
                                    BodyPartGroupDef bpg = thingDef.apparel.bodyPartGroups[index];
                                    if (index > 0)
                                    {
                                        tooltip += ", ";
                                    }

                                    tooltip += bpg.LabelCap;
                                }

                                tooltip += "\n";
                                for (int index = 0; index < thingDef.apparel.layers.Count; index++)
                                {
                                    ApparelLayer layer = thingDef.apparel.layers[index];
                                    if (index > 0)
                                    {
                                        tooltip += ", ";
                                    }

                                    tooltip += layer.ToString();
                                }

                                List<StatModifier> statBases =
                                    thingDef.statBases.Where(bing => bing.stat.category == StatCategoryDefOf.Apparel)
                                        .ToList();
                                if (!statBases.NullOrEmpty())
                                {
                                    // tooltip = StatCategoryDefOf.Apparel.LabelCap;
                                    // tooltip += "\n-------------------------------";
                                    tooltip += "\n";
                                    for (int index = 0; index < statBases.Count; index++)
                                    {
                                        StatModifier statOffset = statBases[index];
                                        {
                                            // if (index > 0)
                                            tooltip += "\n";
                                        }

                                        tooltip += statOffset.stat.LabelCap + Separator
                                                   + statOffset.ValueToStringAsOffset;
                                    }
                                }

                                if (!thingDef.equippedStatOffsets.NullOrEmpty())
                                {
                                    // if (tooltip == string.Empty)
                                    // {
                                    // tooltip = StatCategoryDefOf.EquippedStatOffsets.LabelCap;
                                    // }
                                    {
                                        // else
                                        tooltip += "\n\n" + StatCategoryDefOf.EquippedStatOffsets.LabelCap;
                                    }

                                    tooltip += newLine;
                                    foreach (StatModifier statOffset in thingDef.equippedStatOffsets)
                                    {
                                        tooltip += "\n";
                                        tooltip += statOffset.stat.LabelCap + Separator
                                                   + statOffset.ValueToStringAsOffset;
                                    }
                                }

                                if (colonistsWithThing.Count > 0)
                                {
                                    tooltip += "\n\nWorn by: ";
                                    for (int j = 0; j < colonistsWithThing.Count; j++)
                                    {
                                        Pawn p = colonistsWithThing[j];
                                        if (j > 0)
                                        {
                                            tooltip += j != colonistsWithThing.Count - 1 ? ", " : " and ";
                                        }

                                        tooltip += p.LabelShort;
                                    }
                                }

                                TooltipHandler.TipRegion(
                                    new Rect(
                                        Event.current.mousePosition.x - 5f,
                                        Event.current.mousePosition.y - 5f,
                                        10f,
                                        10f),
                                    tooltip);
                            }

                            void Action()
                            {
                                bool any = false;
                                foreach (Pawn col in selTable.Map.mapPawns.FreeColonists)
                                {
                                    if (recipe.PawnSatisfiesSkillRequirements(col))
                                    {
                                        any = true;
                                        break;
                                    }
                                }
                                if (!any)
                                {
                                    Bill.CreateNoPawnsWithSkillDialog(recipe);
                                }

                                Bill bill = recipe.MakeNewBill();
                                selTable.billStack.AddBill(bill);
                                if (recipe.conceptLearned != null)
                                {
                                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(
                                        recipe.conceptLearned,
                                        KnowledgeAmount.Total);
                                }

                                if (TutorSystem.TutorialMode)
                                {
                                    TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap);
                                }
                            }

                            FloatMenuOption floatMenuOption = new FloatMenuOption(
                                recipe.LabelCap,
                                Action,
                                MenuOptionPriority.Default,
                                MouseoverGuiAction,
                                null,
                                29f,
                                rect => Widgets.InfoCardButton(
                                    (float)(rect.x + 5.0),
                                    (float)(rect.y + (rect.height - 24.0) / 2.0),
                                    recipe));

                            // recipe.products?.FirstOrDefault()?.thingDef));

                            // list.Add(new FloatMenuOption("LoL", null));
                            // Outfitter jump in here

                            // for (int j = 0; j < recipe.products.Count; j++)
                            // {
                            int count = selTable.Map.listerThings.ThingsOfDef(recipeProduct.thingDef).Count;

                            int wornCount = colonistsWithThing.Count;

                            for (int k = 0; k < recipeProduct.thingDef?.apparel?.bodyPartGroups?.Count; k++)
                            {
                                BodyPartGroupDef bPart = recipeProduct.thingDef.apparel.bodyPartGroups[k];

                                string key = bPart.LabelCap + Tools.NestedString;

                                if (!dictionary.ContainsKey(key))
                                {
                                    dictionary.Add(key, new List<FloatMenuOption>());
                                }

                                if (k == 0)
                                {
                                    floatMenuOption.Label += " (" + count + "/" + wornCount + ")";

                                    // + "\n"
                                    // + recipeProduct.thingDef.equippedStatOffsets.ToStringSafeEnumerable();
                                }

                                dictionary[key].Add(floatMenuOption);
                            }
                        }
                    }

                    // Dictionary<string, List<FloatMenuOption>> list2 = new Dictionary<string, List<FloatMenuOption>>();
                    // dictionary2 = dictionary2.OrderByDescending(c => c.Key).ToDictionary(KeyValuePair<string, List<FloatMenuOption>>);
                    if (!dictionary.Any())
                    {
                        dictionary.Add("NoneBrackets".Translate(), new List<FloatMenuOption>() { null });
                    }

                    // else
                    // {
                    // foreach (KeyValuePair<string, List<FloatMenuOption>> pair in list)
                    // {
                    // string label = pair.Key;
                    // if (pair.Value.Count == 1)
                    // {
                    // label = pair.Value.FirstOrDefault().Label;
                    // }
                    // list2.Add(label, pair.Value);
                    // }
                    // }
                    return dictionary;
                };

            mouseoverBill = DoListing(selTable.BillStack, rect2, labeledSortingActions, ref scrollPosition, ref viewHeight);

            return false;
        }

        public static bool TabUpdate_Prefix()
        {
            Building_WorkTable selTable = (Building_WorkTable)Find.Selector.SingleSelectedThing;
            if (selTable.def != ThingDef.Named("HandTailoringBench")
                && selTable.def != ThingDef.Named("ElectricTailoringBench"))
            {
                return true;
            }

            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(Find.Selector.SingleSelectedThing.Position);
                mouseoverBill = null;
            }

            return false;
        }

        public static Bill DoListing(BillStack __instance, Rect rect, Func<Dictionary<string, List<FloatMenuOption>>> labeledSortingActions, ref Vector2 scrollPosition, ref float viewHeight)
        {
            Bill result = null;
            GUI.BeginGroup(rect);
            Text.Font = GameFont.Small;
            if (__instance.Count < 15)
            {
                Rect rect2 = new Rect(0f, 0f, 150f, 29f);
                if (Widgets.ButtonText(rect2, "AddBill".Translate()))
                {
                    // Outfitter Code
                    List<FloatMenuOption> items = labeledSortingActions.Invoke().Keys.Select(
                        label =>
                            {
                                List<FloatMenuOption> fmo = labeledSortingActions.Invoke()[label];
                                return Tools.MakeMenuItemForLabel(label, fmo);
                            }).ToList();

                    Tools.LabelMenu = new FloatMenuLabels(items);
                    Find.WindowStack.Add(Tools.LabelMenu);

                    // Vanilla
                    // Find.WindowStack.Add(new FloatMenu(recipeOptionsMaker()));
                }

                UIHighlighter.HighlightOpportunity(rect2, "AddBill");
            }

            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 35f, rect.width, (float)(rect.height - 35.0));
            Rect viewRect = new Rect(0f, 0f, (float)(outRect.width - 16.0), viewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float num = 0f;
            for (int i = 0; i < __instance.Count; i++)
            {
                Bill bill = __instance.Bills[i];

                Rect rect3 = bill.DoInterface(0f, num, viewRect.width, i);
                if (!bill.DeletedOrDereferenced && Mouse.IsOver(rect3))
                {
                    result = bill;
                }

                num = (float)(num + (rect3.height + 6.0));
            }

            if (Event.current.type == EventType.Layout)
            {
                viewHeight = (float)(num + 60.0);
            }

            Widgets.EndScrollView();
            GUI.EndGroup();
            DoBWMPostfix?.Invoke(ref __instance, ref rect);
            return result;
        }

        public delegate void Postfix(ref BillStack __instance, ref Rect rect);

        public static event Postfix DoBWMPostfix;

    }
}