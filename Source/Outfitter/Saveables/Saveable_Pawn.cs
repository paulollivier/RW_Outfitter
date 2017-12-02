namespace Outfitter
{
    using System.Collections.Generic;

    using Outfitter.Enums;

    using RimWorld;

    using Verse;

    public class SaveablePawn : IExposable
    {
        public List<Saveable_Pawn_StatDef> ApparelStats = new List<Saveable_Pawn_StatDef>();

        public List<Apparel> toWear = new List<Apparel>();

        public List<Apparel> toDrop = new List<Apparel>();

        public bool armorOnly;

        public bool AutoEquipWeapon;

        // public FloatRange RealComfyTemperatures;
        private bool forceStatUpdate;

        public bool ForceStatUpdate
        {
            get
            {
                return this.forceStatUpdate;
            }

            set
            {
                this.forceStatUpdate = value;

            }
        }

        public MainJob mainJob;

        // Exposed members
        public Pawn Pawn;

        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        public FloatRange TargetTemperatures;

        public bool TargetTemperaturesOverride;

        public FloatRange Temperatureweight;

        private bool addIndividualStats = true;
        private bool addPersonalStats = true;

        private bool addWorkStats = true;

        public bool AddIndividualStats
        {
            get => this.addIndividualStats;
            set
            {
                this.addIndividualStats = value;
            }
        }
        public bool AddPersonalStats
        {
            get => this.addPersonalStats;
            set
            {
                this.addPersonalStats = value;
            }
        }

        public bool AddWorkStats
        {
            get => this.addWorkStats;

            set
            {
                this.addWorkStats = value;
            }
        }

        // public SaveablePawn(Pawn pawn)
        // {
        // Pawn = pawn;
        // Stats = new List<Saveable_Pawn_StatDef>();
        // _lastStatUpdate = -5000;
        // _lastTempUpdate = -5000;
        // }
        public void ExposeData()
        {
            Scribe_References.Look(ref this.Pawn, "Pawn");
            Scribe_Values.Look(ref this.TargetTemperaturesOverride, "targetTemperaturesOverride");
            Scribe_Values.Look(ref this.TargetTemperatures, "TargetTemperatures");

            // bug: stats are not saved
            Scribe_Collections.Look(ref this.Stats, "Stats", LookMode.Deep);

            // todo: rename with next big version
            Scribe_Collections.Look(ref this.ApparelStats, "WeaponStats", LookMode.Deep);
            Scribe_Values.Look(ref this.addWorkStats, "AddWorkStats", true);
            Scribe_Values.Look(ref this.addIndividualStats, "AddIndividualStats", true);
            Scribe_Values.Look(ref this.addPersonalStats, "addPersonalStats", true);
            Scribe_Values.Look(ref this.mainJob, "mainJob");
        }
    }
}