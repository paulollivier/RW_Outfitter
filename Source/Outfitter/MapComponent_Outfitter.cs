using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Outfitter
{
     class MapComponent_Outfitter : MapComponent
    {
         private List<SaveablePawn> _pawnCache = new List<SaveablePawn>();

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in _pawnCache)
                if (c.Pawn == pawn)
                    return c;
            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            _pawnCache.Add(n);
            return n;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            //     PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public static MapComponent_Outfitter Get
        {
            get
            {
                MapComponent_Outfitter getComponent = Find.VisibleMap.components.OfType<MapComponent_Outfitter>().FirstOrDefault();
                if (getComponent != null)
                {
                    return getComponent;
                }
                getComponent = new MapComponent_Outfitter(Find.VisibleMap);
                Find.VisibleMap.components.Add(getComponent);

                return getComponent;
            }
        }


        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _pawnCache, "Pawns", LookMode.Deep);

            if (_pawnCache == null)
                _pawnCache = new List<SaveablePawn>();
        }

        public MapComponent_Outfitter(Map map)
            : base(map)
        {
            this.map = map;
        }
    }
}
