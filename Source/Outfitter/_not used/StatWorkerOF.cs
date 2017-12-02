using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Outfitter
{
    using RimWorld;

    using UnityEngine;

    using Verse;

    public  class StatWorkerOF : StatWorker
    {


        public  float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (Prefs.DevMode && this.IsDisabledFor(req.Thing))
            {
                Log.ErrorOnce(string.Format("Attempted to calculate value for disabled stat {0}; this is meant as a consistency check, either set the stat to neverDisabled or ensure this pawn cannot accidentally use this stat (thing={1})", this.stat, req.Thing.ToStringSafe()), 75193282 + this.stat.index);
            }

            float num = this.GetBaseValueFor(req.Def);
            Pawn pawn = req.Thing as Pawn;
            if (pawn != null)
            {
                if (pawn.skills != null)
                {
                    if (this.stat.skillNeedOffsets != null)
                    {
                        for (int i = 0; i < this.stat.skillNeedOffsets.Count; i++)
                        {
                            num += this.stat.skillNeedOffsets[i].ValueFor(pawn);
                        }
                    }
                }
                else
                {
                    num += this.stat.noSkillOffset;
                }

                if (this.stat.capacityOffsets != null)
                {
                    for (int j = 0; j < this.stat.capacityOffsets.Count; j++)
                    {
                        PawnCapacityOffset pawnCapacityOffset = this.stat.capacityOffsets[j];
                        num += pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(pawnCapacityOffset.capacity));
                    }
                }

                if (pawn.story != null)
                {
                    for (int k = 0; k < pawn.story.traits.allTraits.Count; k++)
                    {
                        num += pawn.story.traits.allTraits[k].OffsetOfStat(this.stat);
                    }
                }

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int l = 0; l < hediffs.Count; l++)
                {
                    HediffStage curStage = hediffs[l].CurStage;
                    if (curStage != null)
                    {
                        num += curStage.statOffsets.GetStatOffsetFromList(this.stat);
                    }
                }

                if (pawn.apparel != null)
                {
                    for (int m = 0; m < pawn.apparel.WornApparel.Count; m++)
                    {
                        num += StatWorker.StatOffsetFromGear(pawn.apparel.WornApparel[m], this.stat);
                    }
                }

                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    num += StatWorker.StatOffsetFromGear(pawn.equipment.Primary, this.stat);
                }

                if (pawn.story != null)
                {
                    for (int n = 0; n < pawn.story.traits.allTraits.Count; n++)
                    {
                        num *= pawn.story.traits.allTraits[n].MultiplierOfStat(this.stat);
                    }
                }

                num *= pawn.ageTracker.CurLifeStage.statFactors.GetStatFactorFromList(this.stat);
            }

            if (req.StuffDef != null && (num > 0.0 || this.stat.applyFactorsIfNegative))
            {
                num += req.StuffDef.stuffProps.statOffsets.GetStatOffsetFromList(this.stat);
                num *= req.StuffDef.stuffProps.statFactors.GetStatFactorFromList(this.stat);
            }

            if (req.HasThing)
            {
                CompAffectedByFacilities compAffectedByFacilities = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (compAffectedByFacilities != null)
                {
                    num += compAffectedByFacilities.GetStatOffset(this.stat);
                }

                if (this.stat.statFactors != null)
                {
                    for (int num2 = 0; num2 < this.stat.statFactors.Count; num2++)
                    {
                        num *= req.Thing.GetStatValue(this.stat.statFactors[num2], true);
                    }
                }

                if (pawn != null)
                {
                    if (pawn.skills != null)
                    {
                        if (this.stat.skillNeedFactors != null)
                        {
                            for (int num3 = 0; num3 < this.stat.skillNeedFactors.Count; num3++)
                            {
                                num *= this.stat.skillNeedFactors[num3].ValueFor(pawn);
                            }
                        }
                    }
                    else
                    {
                        num *= this.stat.noSkillFactor;
                    }

                    if (this.stat.capacityFactors != null)
                    {
                        for (int num4 = 0; num4 < this.stat.capacityFactors.Count; num4++)
                        {
                            PawnCapacityFactor pawnCapacityFactor = this.stat.capacityFactors[num4];
                            float factor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(pawnCapacityFactor.capacity));
                            num = Mathf.Lerp(num, num * factor, pawnCapacityFactor.weight);
                        }
                    }

                    if (pawn.Inspired)
                    {
                        num += pawn.InspirationDef.statOffsets.GetStatOffsetFromList(this.stat);
                        num *= pawn.InspirationDef.statFactors.GetStatFactorFromList(this.stat);
                    }
                }
            }

            return num;
        }

    }
}
