using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outfitter
{
    using System.Reflection;

    using Harmony;

    using RimWorld;

    using UnityEngine;

    using Verse;

    using FloatMenuOption = Verse.FloatMenuOption;

    public static class ITab_Bills_Patch
    {
        private static float viewHeight = 1000f;

        private static Vector2 scrollPosition = default(Vector2);

        // RimWorld.ITab_Bills
        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        public static bool FillTab_Prefix()
        {
            PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.BillsTab, KnowledgeAmount.FrameDisplayed);
            Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);
            Func<List<FloatMenuOption>> recipeOptionsMaker = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    for (int i = 0; i < SelTable.def.AllRecipes.Count; i++)
                    {
                        list.Add(new FloatMenuOption("LOL", null));

                        if (SelTable.def.AllRecipes[i].AvailableNow)
                        {
                            RecipeDef recipe = SelTable.def.AllRecipes[i];
                            list.Add(new FloatMenuOption(recipe.LabelCap, delegate
                                {
                                    if (!SelTable.Map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                                    {
                                        Bill.CreateNoPawnsWithSkillDialog(recipe);
                                    }
                                    Bill bill = recipe.MakeNewBill();
                                    SelTable.billStack.AddBill(bill);
                                    if (recipe.conceptLearned != null)
                                    {
                                        PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                                    }
                                    if (TutorSystem.TutorialMode)
                                    {
                                        TutorSystem.Notify_Event("AddBill-" + recipe.LabelCap);
                                    }
                                }));
                        }
                    }
                    if (!Enumerable.Any<FloatMenuOption>(list))
                    {
                        list.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                    }

                    return list;
                };

            Bill mouseoverBill = SelTable.billStack.DoListing(rect, recipeOptionsMaker, ref scrollPosition, ref viewHeight);

            typeof(Bill).GetField(
                "mouseoverBill",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(typeof(Bill), mouseoverBill);
            return false;
        }
    }
}