namespace Outfitter.Optional
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Harmony;

    using Verse;

    // Blatantly stolen from "Psychology"
    [StaticConstructorOnStartup]
    internal static class Harmony_DDWorkTab
    {
        static Harmony_DDWorkTab()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("rimworld.outfitter.ddworktab_patch");
            if (ModLister.AllInstalledMods.Any(x => x.Active && x.Identifier.Equals("DD_WorkTab")))
            {
                harmony.Patch(
                    AccessTools.Method(typeof(DD_WorkTab.Draggables.PawnSurface), "UpdatePawnPriorities"),
                    null,
                    new HarmonyMethod(typeof(Harmony_DDWorkTab), nameof(UpdatePriorities)));

                harmony.Patch(
                    AccessTools.Method(typeof(ApparelStatsHelper), nameof(ApparelStatsHelper.GetWorkPriority)),
                    new HarmonyMethod(typeof(Harmony_DDWorkTab), nameof(GetWorkPriorityDD)),
                    null);
            }
        }

        private static List<string> ignoreList =
            new List<string> { "Firefighter", "Patient", "PatientBedRest", "Flicker", "HaulingUrgent", "FinishingOff" };

        private static bool GetWorkPriorityDD(Pawn pawn, WorkTypeDef workType, ref int __result)
        {
            __result = 20;

            DD_WorkTab.Draggables.SurfaceManager GetManager = Current.Game.GetComponent<DD_WorkTab.Draggables.SurfaceManager>();
            DD_WorkTab.Draggables.PawnSurface surface = GetManager.GetPawnSurface(pawn);

            FieldInfo childrenFieldInfo =
                typeof(DD_WorkTab.Draggables.PawnSurface).GetField("children", BindingFlags.NonPublic | BindingFlags.Instance);
            List<DD_WorkTab.Draggables.DraggableWork> children = (List<DD_WorkTab.Draggables.DraggableWork>)childrenFieldInfo?.GetValue(surface);

            if (children.NullOrEmpty())
            {
                return true;
            }


            List<DD_WorkTab.Draggables.DraggableWork> filtered = children.Where(x => !x.Disabled && !ignoreList.Contains(x.Def.defName))
                .ToList();

            if (filtered.NullOrEmpty())
            {
                return true;
            }

            for (int i = 0; i < filtered.Count; i++)
            {
                DD_WorkTab.Draggables.DraggableWork current = filtered[i];

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

        private static void UpdatePriorities(DD_WorkTab.Draggables.PawnSurface __instance)
        {
            __instance.pawn.GetSaveablePawn().forceStatUpdate = true;
        }
    }
}