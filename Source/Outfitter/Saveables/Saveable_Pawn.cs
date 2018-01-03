using System.Collections.Generic;
using Outfitter.Enums;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class SaveablePawn : IExposable
    {
        public List<Saveable_Pawn_StatDef> ApparelStats = new List<Saveable_Pawn_StatDef>();

        public List<Apparel> ToWear = new List<Apparel>();

        public List<Apparel> ToDrop = new List<Apparel>();

        public bool ArmorOnly;

        public bool AutoEquipWeapon;

        // public FloatRange RealComfyTemperatures;
        private bool _forceStatUpdate;

        public bool ForceStatUpdate
        {
            get
            {
                return this._forceStatUpdate;
            }

            set
            {
                this._forceStatUpdate = value;

            }
        }

        public MainJob MainJob;

        // Exposed members
        public Pawn Pawn;

        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        public FloatRange TargetTemperatures;

        public bool TargetTemperaturesOverride;

        public FloatRange Temperatureweight;

        private bool _addIndividualStats = true;
        private bool _addPersonalStats = true;

        private bool _addWorkStats = true;

        public bool AddIndividualStats
        {
            get => this._addIndividualStats;
            set
            {
                this._addIndividualStats = value;
            }
        }
        public bool AddPersonalStats
        {
            get => this._addPersonalStats;
            set
            {
                this._addPersonalStats = value;
            }
        }

        public bool AddWorkStats
        {
            get => this._addWorkStats;

            set
            {
                this._addWorkStats = value;
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
            Scribe_Values.Look(ref this._addWorkStats, "AddWorkStats", true);
            Scribe_Values.Look(ref this._addIndividualStats, "AddIndividualStats", true);
            Scribe_Values.Look(ref this._addPersonalStats, "addPersonalStats", true);
            Scribe_Values.Look(ref this.MainJob, "mainJob");
        }
    }
}