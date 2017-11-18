namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using RimWorld;

    using UnityEngine;

    using Verse;

    public static class ITab_Bills_Patch
    {
        private static float viewHeight = 1000f;

        private static Vector2 scrollPosition = default(Vector2);

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        private static Bill mouseoverBill;

        // RimWorld.ITab_Bills

        public static bool FillTab_Prefix()
        {
            Building_WorkTable selTable = (Building_WorkTable)Find.Selector.SingleSelectedThing;

            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            float x = WinSize.x;
            Vector2 winSize2 = WinSize;
            Rect rect2 = new Rect(0f, 0f, x, winSize2.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    for (int i = 0; i < selTable.def.AllRecipes.Count; i++)
                    {
                        if (selTable.def.AllRecipes[i].AvailableNow)
                        {
                            RecipeDef recipe = selTable.def.AllRecipes[i];
                            list.Add(
                                new FloatMenuOption(
                                    recipe.LabelCap,
                                    delegate
                                        {
                                            if (!selTable.Map.mapPawns.FreeColonists.Any(
                                                    col => recipe.PawnSatisfiesSkillRequirements(col)))
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
                                        },
                                    MenuOptionPriority.Default,
                                    null,
                                    null,
                                    29f,
                                    rect => Widgets.InfoCardButton(
                                        (float)(rect.x + 5.0),
                                        (float)(rect.y + (rect.height - 24.0) / 2.0),
                                        recipe)));
                        }
                    }

                    if (!list.Any())
                    {
                        list.Add(
                            new FloatMenuOption(
                                "NoneBrackets".Translate(),
                                null));
                    }

                    return list;
                };

             mouseoverBill = selTable.billStack.DoListing(rect2, recipeOptionsMaker, ref scrollPosition, ref viewHeight);


            return false;

        }

        public static bool TabUpdate_Prefix()
        {
            if (mouseoverBill != null)
            {
                mouseoverBill.TryDrawIngredientSearchRadiusOnMap(Find.Selector.SingleSelectedThing.Position);
                mouseoverBill = null;
            }
            return false;
        }
    }
}