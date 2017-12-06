using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outfitter.Optional
{
    using Harmony;

    using Outfitter.TabPatch;

    using Verse;

    public class PatchBW : GameComponent
    {
        public PatchBW(Game game)
        {
            try
            {
                ((Action)(() =>
                    {
                        if (AccessTools.Method(
                                typeof(ImprovedWorkbenches.BillStack_DoListing_Detour),
                                nameof(ImprovedWorkbenches.BillStack_DoListing_Detour.Postfix)) != null)
                        {
                            ITab_Bills_Patch.DoBWMPostfix += ImprovedWorkbenches.BillStack_DoListing_Detour.Postfix;
                        }
                    }))();
            }
            catch (TypeLoadException)
            {
            }
        }
    }
}
