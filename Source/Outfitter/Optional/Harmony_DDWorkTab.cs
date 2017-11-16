namespace Outfitter.Optional
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using DD_WorkTab.Draggables;

    using Harmony;

    using RimWorld;

    using Verse;

    using Extensions = Outfitter.Extensions;

    // Blatantly stolen from "Psychology"
    [StaticConstructorOnStartup]
    internal static class Harmony_DDWorkTab
    {
        static Harmony_DDWorkTab()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.outfitter.ddworktab_patch");
            try
            {
                ((Action)(() =>
                    {
                        if (AccessTools.Method(
                                typeof(PawnSurface),
                                nameof(PawnSurface.EnableWorkType)) != null)
                        {
                             harmony.Patch(
                                 AccessTools.Method(typeof(PawnSurface), "UpdatePawnPriorities"),
                                 null,
                                 new HarmonyMethod(typeof(Harmony_DDWorkTab), nameof(UpdatePriorities)));

                            harmony.Patch(
                                AccessTools.Method(typeof(ApparelStatsHelper), nameof(Extensions.GetWorkPriority)),
                                new HarmonyMethod(typeof(Harmony_DDWorkTab), nameof(GetWorkPriorityDD)),
                                null);
                        }

                        // harmony.Patch(
                        // AccessTools.Method(typeof(EdB.PrepareCarefully.DialogManageImplants), "InitializeWithPawn"),
                        // null,
                        // new HarmonyMethod(
                        // typeof(DialogManageImplantsPatch),
                        // nameof(DialogManageImplantsPatch.InitializeWithPawn)));
                        // harmony.Patch(
                        // AccessTools.Method(typeof(EdB.PrepareCarefully.DialogManageImplants), "AddImplant"),
                        // new HarmonyMethod(
                        // typeof(DialogManageImplantsPatch),
                        // nameof(DialogManageImplantsPatch.UpdatePawnForImpplants)),
                        // null);
                        // harmony.Patch(
                        // AccessTools.Method(typeof(EdB.PrepareCarefully.DialogManageImplants), "RemoveImplant"),
                        // new HarmonyMethod(
                        // typeof(DialogManageImplantsPatch),
                        // nameof(DialogManageImplantsPatch.UpdatePawnForImpplants)),
                        // null);
                    }))();
            }
            catch (TypeLoadException)
            {
            }
        }

        private static void UpdatePriorities(PawnSurface __instance)
        {
            __instance.pawn.GetSaveablePawn().forceStatUpdate = true;
        }

        private static bool GetWorkPriorityDD(Pawn pawn, WorkTypeDef workType, ref int __result)
        {
            __result = 20;

            var GetManager = Current.Game.GetComponent<SurfaceManager>();
            PawnSurface surface = GetManager.GetPawnSurface(pawn);

            var childrenFieldInfo = typeof(PawnSurface).GetField("children", BindingFlags.NonPublic | BindingFlags.Instance);
            List<DraggableWork> children = (List<DraggableWork>)childrenFieldInfo?.GetValue(surface);

            if (children.NullOrEmpty())
            {
                return true;
            }
            List<string> ignoreList = new List<string>() { "Firefighter", "Patient", "PatientBedRest", "Flicker", "HaulingUrgent", "FinishingOff" };
            List<DraggableWork> filtered = children.Where(x => !x.Disabled && !ignoreList.Contains(x.Def.defName)).ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                DraggableWork current = filtered[i];

                if (current.Def != workType)
                {
                    continue;
                }
                int priority = i + 1;
                __result = priority;
                break;
            }
            return false;
        }
    }
}