using System.Collections.Generic;
using Verse;

namespace Outfitter
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;

        public bool TargetTemperaturesOverride;

        public bool AddWorkStats = true;

        public bool AddIndividualStats = true;

        public FloatRange TargetTemperatures;

        // public FloatRange RealComfyTemperatures;
        public bool forceStatUpdate = false;

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

        public MainJob mainJob;

        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        public List<Saveable_Pawn_StatDef> ApparelStats = new List<Saveable_Pawn_StatDef>();

        public bool SetRealComfyTemperatures;

        public bool AutoEquipWeapon;

        public bool armorOnly = false;

        public FloatRange Temperatureweight;

        public FloatRange RealComfyTemperatures;

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

            Scribe_Values.Look(ref RealComfyTemperatures, "RealComfyTemperatures");
            Scribe_Collections.Look(ref this.Stats, "Stats", LookMode.Deep);

            // to do: rename with next big version
            Scribe_Collections.Look(ref this.ApparelStats, "WeaponStats", LookMode.Deep);
            Scribe_Values.Look(ref this.AddWorkStats, "AddWorkStats", true);
            Scribe_Values.Look(ref this.AddIndividualStats, "AddIndividualStats", true);
            Scribe_Values.Look(ref this.mainJob, "mainJob");
        }
    }
}