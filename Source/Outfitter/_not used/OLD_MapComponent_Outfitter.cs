// ReSharper disable All

namespace Outfitter
{
    using System.Collections.Generic;

    using Verse;

    // TODO: remove mapcomponent in next alpha
    internal class MapComponent_Outfitter : MapComponent
    {
        public List<SaveablePawn> _pawnCacheMap = new List<SaveablePawn>();

        private bool updated;

        public MapComponent_Outfitter(Map map)
            : base(map)
        {
            this.map = map;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref this.updated, "updated");
            Scribe_Collections.Look(ref this._pawnCacheMap, "Pawns", LookMode.Deep);

            if (!this.updated)
            {
                foreach (SaveablePawn c in this._pawnCacheMap)
                {
                    if (GameComponent_Outfitter._pawnCache.Contains(c))
                    {
                        continue;
                    }

                    GameComponent_Outfitter._pawnCache.Add(c);
                }

                this.updated = true;
            }

            // this._pawnCacheMap = null;
        }

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in this._pawnCacheMap)
            {
                if (c.Pawn == pawn)
                {
                    return c;
                }
            }

            return null;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            // PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }
    }
}