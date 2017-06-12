using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Outfitter
{
    // to do: remove mapcomponent in next major version
    class MapComponent_Outfitter : MapComponent
    {
        public List<SaveablePawn> _pawnCacheMap = new List<SaveablePawn>();

        private bool updated;

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in _pawnCacheMap)
                if (c.Pawn == pawn)
                    return c;

            return null;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            //     PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.updated, "updated");
            Scribe_Collections.Look(ref this._pawnCacheMap, "Pawns", LookMode.Deep);

            if (!updated)
            {
                foreach (SaveablePawn c in _pawnCacheMap)
                {
                    if (GameComponent_Outfitter._pawnCache.Contains(c))
                        continue;
                    GameComponent_Outfitter._pawnCache.Add(c);
                }
                updated = true;
            }
            //     this._pawnCacheMap = null;
        }

        public MapComponent_Outfitter(Map map)
            : base(map)
        {
            this.map = map;
        }
    }
}
