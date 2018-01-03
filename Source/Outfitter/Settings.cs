using UnityEngine;
using Verse;

namespace Outfitter
{
    public class Settings : ModSettings
    {
        public bool UseEyes => this._useEyes;
        private bool _useEyes;
        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard { ColumnWidth = inRect.width / 2 };
            list.Begin(inRect);

            list.Gap();

            list.CheckboxLabeled(
                "Settings.UseEyes".Translate(),
                ref this._useEyes,
                "Settings.UseEyesTooltip".Translate());

            list.End();

            if (GUI.changed)
            {
                this.Mod.WriteSettings();
            }

            // FlexibleSpace();
            // BeginVertical();
            // if (Button("Settings.Apply".Translate()))
            // {
            // foreach (Pawn pawn in PawnsFinder.AllMapsAndWorld_Alive)
            // {
            // if (pawn.RaceProps.Humanlike)
            // {
            // CompFace faceComp = pawn.TryGetComp<CompFace>();
            // if (faceComp != null)
            // {
            // this.WriteSettings();
            // faceComp.sessionOptimized = false;
            // pawn.Drawer.renderer.graphics.ResolveAllGraphics();
            // }
            // }
            // }
            // }
            // EndVertical();
            // FlexibleSpace();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this._useEyes, "useEyes", false, true);
        }
    }
}
