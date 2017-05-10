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

        public FloatRange Temperatureweight;

        public FloatRange TargetTemperatures;
        public FloatRange RealComfyTemperatures;

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
        public List<Saveable_Pawn_StatDef> WeaponStats = new List<Saveable_Pawn_StatDef>();
        public bool SetRealComfyTemperatures;
        public bool AutoEquipWeapon;

        //  public SaveablePawn(Pawn pawn)
        //    {
        //        Pawn = pawn;
        //        Stats = new List<Saveable_Pawn_StatDef>();
        //        _lastStatUpdate = -5000;
        //        _lastTempUpdate = -5000;
        //    }


        public void ExposeData()
        {
            Scribe_References.Look(ref Pawn, "Pawn");
            Scribe_Values.Look(ref TargetTemperaturesOverride, "targetTemperaturesOverride");
            Scribe_Values.Look(ref TargetTemperatures, "TargetTemperatures");
            Scribe_Values.Look(ref SetRealComfyTemperatures, "SetRealComfyTemperatures");
            Scribe_Values.Look(ref RealComfyTemperatures, "RealComfyTemperatures");
            Scribe_Values.Look(ref Temperatureweight, "Temperatureweight");
            Scribe_Collections.Look(ref Stats, "Stats", LookMode.Deep);
            Scribe_Collections.Look(ref WeaponStats, "WeaponStats", LookMode.Deep);
            Scribe_Values.Look(ref AddWorkStats, "AddWorkStats", true);
            Scribe_Values.Look(ref AddIndividualStats, "AddIndividualStats", true);
            Scribe_Values.Look(ref mainJob, "mainJob");


        }
    }
}