using Outfitter.Enums;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class Saveable_Pawn_StatDef : IExposable
    {
        private StatAssignment _assignment;

        private StatDef _stat;

        private float _weight;

        public StatAssignment Assignment
        {
            get => this._assignment;
            set => this._assignment = value;
        }

        public StatDef Stat
        {
            get => this._stat;
            set => this._stat = value;
        }

        public float Weight
        {
            get => this._weight;
            set => this._weight = value;
        }

        /*
public Saveable_Pawn_StatDef(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
{
Stat = stat;
Weight = priority;
Assignment = assignment;
}

public Saveable_Pawn_StatDef(KeyValuePair<StatDef, float> statDefWeightPair, StatAssignment assignment = StatAssignment.Automatic)
{
Stat = statDefWeightPair.Key;
Weight = statDefWeightPair.Value;
Assignment = assignment;
}
*/
        public void ExposeData()
        {
            Scribe_Defs.Look(ref this._stat, "Stat");
            Scribe_Values.Look(ref this._assignment, "Assignment");
            Scribe_Values.Look(ref this._weight, "Weight");
        }
    }
}