using RimWorld;
using Verse;

namespace Outfitter
{
    public class Saveable_Pawn_StatDef : IExposable
    {
        private StatDef stat;
        private StatAssignment assignment;
        private float weight;

        public StatDef Stat
        {
            get => this.stat;
            set => this.stat = value;
        }

        public StatAssignment Assignment
        {
            get => this.assignment;
            set => this.assignment = value;
        }

        public float Weight
        {
            get => this.weight;
            set => this.weight = value;
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
            Scribe_Defs.Look(ref this.stat, "Stat");
            Scribe_Values.Look(ref this.assignment, "Assignment");
            Scribe_Values.Look(ref this.weight, "Weight");
        }
    }
}
