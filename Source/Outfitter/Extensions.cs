namespace Outfitter
{
    using System.Linq;

    using Verse;

    public static class Extensions
    {
        public static SaveablePawn GetSaveablePawn(this Pawn pawn)
        {
            GameComponent_Outfitter outfitter = Current.Game.GetComponent<GameComponent_Outfitter>();
            foreach (SaveablePawn c in outfitter._pawnCache.Where(c => c.Pawn == pawn))
            {
                return c;
            }

            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            outfitter._pawnCache.Add(n);
            return n;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            // PawnApparelStatCaches.Add(pawn, new StatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public static int GetWorkPriority(this Pawn pawn, WorkTypeDef workType)
        {
            return pawn.workSettings.GetPriority(workType);
        }
    }
}