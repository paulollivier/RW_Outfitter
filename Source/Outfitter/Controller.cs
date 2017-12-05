using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outfitter
{
    using JetBrains.Annotations;

    using RimWorld;

    using UnityEngine;

    using Verse;

    class Controller : Mod
    {
        public Controller(ModContentPack content)
            : base(content)
        {
            settings = this.GetSettings<Settings>();

        }
        public static Settings settings;

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoWindowContents(inRect);
        }

        [NotNull]
        public override string SettingsCategory()
        {
            return "Outfitter";
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            settings.Write();

            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (BodyDef bodyDef in DefDatabase<BodyDef>.AllDefsListForReading)
                {
                    if (bodyDef.defName != "Human")
                    {
                        continue;
                    }

                    BodyPartRecord neck = bodyDef.corePart.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Neck);
                    BodyPartRecord head = neck?.parts.FirstOrDefault(x => x.def == BodyPartDefOf.Head);
                    if (head == null)
                    {
                        continue;
                    }
                    //    if (!head.groups.Contains(BodyPartGroupDefOf.Eyes))
                    {
                        //     head.groups.Add(BodyPartGroupDefOf.Eyes);
                        BodyPartRecord leftEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.LeftEye);
                        BodyPartRecord rightEye = head.parts.FirstOrDefault(x => x.def == BodyPartDefOf.RightEye);

                        if (settings.useEyes)
                        {
                            leftEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                            rightEye?.groups.Remove(BodyPartGroupDefOf.FullHead);
                            Log.Message("Outfitter removed FullHead from Human eyes.");
                        }
                        else
                        {
                            if (leftEye != null && !leftEye.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                leftEye?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }
                            if (rightEye != null && !rightEye.groups.Contains(BodyPartGroupDefOf.FullHead))
                            {
                                rightEye?.groups.Add(BodyPartGroupDefOf.FullHead);
                            }
                            Log.Message("Outfitter re-added FullHead to Human eyes.");
                        }
                        break;
                    }

                }

            }
        }
    }
}
