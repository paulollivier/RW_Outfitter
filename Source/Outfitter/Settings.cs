using UnityEngine;
using Verse;

namespace Outfitter
{
    public class Settings : ModSettings
    {
        public bool UseEyes => this._useEyes;

        public bool UseCustomTailorWorkbench => this._useCustomTailorWorkbench;

        private bool _useEyes;
        private bool _useCustomTailorWorkbench;

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard { ColumnWidth = inRect.width / 2 };
            list.Begin(inRect);

            list.Gap();

            list.CheckboxLabeled(
                "Settings.UseEyes".Translate(),
                ref this._useEyes,
                "Settings.UseEyesTooltip".Translate());

            list.CheckboxLabeled(
                "Settings.UseTailorWorkbenchUI".Translate(),
                ref this._useCustomTailorWorkbench,
                "Settings.UseTailorWorkbenchUITooltip".Translate());

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
            Scribe_Values.Look(ref this._useCustomTailorWorkbench, "useCustomTailorWorkbench", false, true);
        }
    }
}
