//namespace Outfitter
//{
//    public class Outfitter_ModBase : HugsLib.ModBase
//    {
//        public override string ModIdentifier { get { return "Outfitter"; } }
//    }

using System;

using Harmony;

using Outfitter;
using Outfitter.InfusedStats;

using Verse;

[StaticConstructorOnStartup]
internal class HarmonyPatchInfused
{

    #region Public Constructors

    static HarmonyPatchInfused()
    {
        HarmonyInstance harmony = HarmonyInstance.Create("com.outfitterinfused.rimworld.mod");

        try
        {
            ((Action)(() =>
                {
                    if (AccessTools.Method(typeof(Infused.GenInfusion), nameof(Infused.GenInfusion.TryGetInfusions))
                        == null)
                    {
                        return;
                    }

                    harmony.Patch(
                        AccessTools.Method(
                            typeof(ApparelStatCache),
                            nameof(ApparelStatCache.ApparelScoreRaw_PawnStatsHandlers)),
                        null,
                        new HarmonyMethod(
                            typeof(InfusedStats),
                            nameof(InfusedStats.ApparelScoreRaw_PawnStatsHandlers)));

                    harmony.Patch(
                        AccessTools.Method(
                            typeof(ApparelStatCache),
                            nameof(ApparelStatCache.ApparelScoreRaw_FillInfusedStat)),
                        null,
                        new HarmonyMethod(
                            typeof(InfusedStats),
                            nameof(InfusedStats.ApparelScoreRaw_FillInfusedStat)));

                    harmony.Patch(
                        AccessTools.Method(
                            typeof(ApparelStatCache),
                            nameof(ApparelStatCache.Ignored_WTHandlers)),
                        null,
                        new HarmonyMethod(
                            typeof(InfusedStats),
                            nameof(InfusedStats.Ignored_WTHandlers)));
                    // ApparelStatCache.ApparelScoreRaw_PawnStatsHandlers += InfusedStats.ApparelScoreRaw_PawnStatsHandlers;
                    // ApparelStatCache.ApparelScoreRaw_FillInfusedStat += InfusedStats.ApparelScoreRaw_FillInfusedStat;
                    // ApparelStatCache.Ignored_WTHandlers += InfusedStats.Ignored_WTHandlers;
                }))();
        }
        catch (TypeLoadException)
        {
        }

        Log.Message("Outfitter successfully initialized Infused stats.");
    }

    #endregion Public Constructors

}