//namespace Outfitter
//{
//    public class Outfitter_ModBase : HugsLib.ModBase
//    {
//        public override string ModIdentifier { get { return "Outfitter"; } }
//    }

using System.Linq;

using Harmony;

using Outfitter;
using Outfitter.Infused;

using RimWorld;

using Verse;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{

    #region Public Constructors

    static HarmonyPatches()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("com.outfitter.rimworld.mod");

        harmony.Patch(
            AccessTools.Method(typeof(InspectPaneUtility), "DoTabs"),
            new HarmonyMethod(typeof(DoTabs_Patch), nameof(DoTabs_Patch.DoTabs_Prefix)),
            null);

        harmony.Patch(
            AccessTools.Method(typeof(JobGiver_OptimizeApparel), "TryGiveJob"),
            new HarmonyMethod(
                typeof(Outfitter_JobGiver_OptimizeApparel),
                nameof(Outfitter_JobGiver_OptimizeApparel.TryGiveJob_Prefix)),
            null);

        if (AccessTools.Method(
                typeof(Infused.GenInfusion),
                nameof(Infused.GenInfusion.TryGetInfusions)) != null)
        {
            InfusedStats.InfusedIsActive = true;
        }

            Log.Message(
            "Outfitter successfully completed " + harmony.GetPatchedMethods().Count() + " patches with harmony.");
    }

    #endregion Public Constructors

}