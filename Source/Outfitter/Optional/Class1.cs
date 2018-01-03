using System;
using Harmony;
using ImprovedWorkbenches;
using Outfitter.TabPatch;
using Verse;

namespace Outfitter.Optional
{
    public class PatchBw : GameComponent
    {
        private readonly Game _game;

        public PatchBw(Game game)
        {
            this._game = game;
            try
            {
                ((Action)(() =>
                    {
                        if (AccessTools.Method(
                                typeof(BillStack_DoListing_Detour),
                                nameof(BillStack_DoListing_Detour.Postfix)) != null)
                        {
                            Tab_Bills_Patch.DoBwmPostfix += BillStack_DoListing_Detour.Postfix;
                        }
                    }))();
            }
            catch (TypeLoadException)
            {
            }
        }
    }
}
