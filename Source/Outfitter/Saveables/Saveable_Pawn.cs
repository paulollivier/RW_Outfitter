using System.Collections.Generic;
using Verse;

namespace Outfitter
{
    public class SaveablePawn : IExposable
    {
        #region Public Fields

        public bool AddIndividualStats = true;

        public bool AddWorkStats = true;

        public List<Saveable_Pawn_StatDef> ApparelStats = new List<Saveable_Pawn_StatDef>();

        public bool armorOnly = false;

        public bool AutoEquipWeapon;

        // public FloatRange RealComfyTemperatures;
        public bool forceStatUpdate = false;

        public MainJob mainJob;

        // Exposed members
        public Pawn Pawn;

        public FloatRange RealComfyTemperatures;
        public bool SetRealComfyTemperatures;
        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();
        public FloatRange TargetTemperatures;
        public bool TargetTemperaturesOverride;
        public FloatRange Temperatureweight;

        #endregion Public Fields

        #region Public Enums

        public enum MainJob
        {
            Anything,

            Soldier00Close_Combat,

            Soldier00Ranged_Combat,

            Artist,

            Constructor,

            Cook,

            Crafter,

            Doctor,

            Grower,

            Handler,

            Hauler,

            Hunter,

            Miner,

            Researcher,

            Smith,

            Tailor,

            Warden
        }

        #endregion Public Enums

        #region Public Methods

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
            Scribe_Values.Look(ref this.SetRealComfyTemperatures, "SetRealComfyTemperatures");

            Scribe_Values.Look(ref this.RealComfyTemperatures, "RealComfyTemperatures");
            // bug: stats are not saved
            Scribe_Collections.Look(ref this.Stats, "Stats", LookMode.Deep);

            // todo: rename with next big version
            Scribe_Collections.Look(ref this.ApparelStats, "WeaponStats", LookMode.Deep);
            Scribe_Values.Look(ref this.AddWorkStats, "AddWorkStats", true);
            Scribe_Values.Look(ref this.AddIndividualStats, "AddIndividualStats", true);
            Scribe_Values.Look(ref this.mainJob, "mainJob");
        }

        #endregion Public Methods
    }
}