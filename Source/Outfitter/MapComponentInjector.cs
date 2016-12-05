
using System;
using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace Outfitter       // Replace with yours.
{       
    // This code is mostly borrowed from Pawn State Icons mod by Dan Sadler, which has open source and no license I could find, so...
    [StaticConstructorOnStartup]
    public class MapComponentInjector : MonoBehaviour
    {
        private static readonly Type outfitter = typeof(MapComponent_Outfitter);


        #region No editing required


        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.MapPlaying)
            {
                return;
            }


            if (Find.Map.components.FindAll(c => c.GetType() == outfitter).Count == 0)
            {
                Find.Map.components.Add((MapComponent)Activator.CreateInstance(outfitter));

                Log.Message("Outfitter :: Added Stats to the map.");
            }
            Destroy(this);
        }

    }
}
#endregion