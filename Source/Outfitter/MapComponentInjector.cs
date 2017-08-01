
using System;
using UnityEngine;
using Verse;

namespace Outfitter
{
    // Replace with yours.
    // This code is mostly borrowed from Pawn State Icons mod by Dan Sadler, which has open source and no license I could find, so...
    [StaticConstructorOnStartup]
    public class MapComponentInjector : MonoBehaviour
    {
        private static readonly Type outfitter = typeof(MapComponent_Outfitter);

        #region No editing required

        // ReSharper disable once UnusedMember.Global
        public void FixedUpdate()
        {
            if (Current.ProgramState != ProgramState.Playing)
            {
                return;
            }

            if (Find.VisibleMap.components.FindAll(c => c.GetType() == outfitter).Count == 0)
            {
                Find.VisibleMap.components.Add((MapComponent)Activator.CreateInstance(outfitter));

                Log.Message("Outfitter :: Added Stats to the map.");
            }

            Destroy(this);
        }

        private void Start()
        {
            GameObject initializer = new GameObject("OutfitterMapComponentInjector");
            initializer.AddComponent<MapComponentInjector>();
            UnityEngine.Object.DontDestroyOnLoad(initializer);
        }
    }
}

#endregion