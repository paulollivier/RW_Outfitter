namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using RimWorld;

    using Verse;

    public class GameComponent_Outfitter : GameComponent
    {
        [NotNull]
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

        public GameComponent_Outfitter()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public GameComponent_Outfitter(Game game)
        {
            if (Controller.settings.useEyes)
            {
                foreach (BodyDef bodyDef in DefDatabase<BodyDef>.AllDefsListForReading)
                {
                    if (bodyDef.defName != "Human")
                    {
                        continue;
                    }

                    BodyPartRecord neck = bodyDef.corePart.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Neck);
                    BodyPartRecord head = neck?.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Head);
                    if (head == null)
                    {
                        continue;
                    }
                    //    if (!head.groups.Contains(BodyPartGroupDefOf.Eyes))
                    {
                        //     head.groups.Add(BodyPartGroupDefOf.Eyes);
                        BodyPartRecord leftEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.LeftEye);
                        BodyPartRecord rightEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.RightEye);
                        leftEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        rightEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                        Log.Message("Outfitter patched Human eyes.");
                        break;
                    }

                }
            }

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where(
                td => td.category == ThingCategory.Pawn && td.race.Humanlike))
            {
                if (def.inspectorTabs == null)
                {
                    def.inspectorTabs = new List<Type>();
                }

                if (def.inspectorTabsResolved == null)
                {
                    def.inspectorTabsResolved = new List<InspectTabBase>();
                }

                if (def.inspectorTabs.Contains(typeof(ITab_Pawn_Outfitter)))
                {
                    return;
                }

                def.inspectorTabs.Add(typeof(ITab_Pawn_Outfitter));
                def.inspectorTabsResolved.Add(InspectTabManager.GetSharedInstance(typeof(ITab_Pawn_Outfitter)));
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref this.PawnCache, "Pawns", LookMode.Deep);
        }
    }
}