namespace Outfitter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Verse;

    public class GameComponent_Outfitter : GameComponent
    {
        public static bool updated;

        [NotNull]
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

        public GameComponent_Outfitter()
        {
        }

        // ReSharper disable once UnusedMember.Global
        public GameComponent_Outfitter(Game game)
        {
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