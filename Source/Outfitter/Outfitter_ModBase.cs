//namespace Outfitter
//{
//    public class Outfitter_ModBase : HugsLib.ModBase
//    {
//        public override string ModIdentifier { get { return "Outfitter"; } }
//    }
//}

using System.Reflection;

using Harmony;

using Verse;

[StaticConstructorOnStartup]
class HarmonyPatches
{

    static HarmonyPatches()
    {
        var harmony = HarmonyInstance.Create("com.outfitter.rimworld.mod");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Log.Message("Outfitter: Adding Harmony Prefix to InspectPaneUtility.DoTabs.");

    }
}