using System.Linq;
using System.Reflection;
using Verse;

namespace Outfitter.NoCCL
{
    public class SpecialInjector
    {

#if NoCCL
        // ReSharper disable once UnusedMemberInSuper.Global
        public virtual bool Inject()
        {
            Log.Error("This should never be called.");
            return false;
        }
    }

    [StaticConstructorOnStartup]
    // ReSharper disable once UnusedMember.Global
    internal static class DetourInjector
    {
        private static Assembly Assembly { get { return Assembly.GetAssembly(typeof(DetourInjector)); } }
        private static string AssemblyName { get { return Assembly.FullName.Split(',').First(); } }
        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "Initializing", true, null);
        }

        private static void Inject()
        {
            OF_SpecialInjector injector = new OF_SpecialInjector();
            if (injector.Inject()) Log.Message(AssemblyName + " injected.");
            else Log.Error(AssemblyName + " failed to get injected properly.");
        }
#endif
    }
}