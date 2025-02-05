﻿using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Isekaiob
{
    /// <summary>
    /// Made to Maintain Same behaviour of Vanilla Rimworld.
    /// </summary>
    public class CompProperties_BreakdownableEx : CompProperties
    {
        /// <summary>
        /// Rimworld CompBreakdownable default Breakdown MTB.
        /// </summary>
        public int BreakdownMTBTicks = 13680000;
        /// <summary>
        /// Rimworld JobDriver_FixBrokenDownBuildingEx default time used to fix building.
        /// </summary>
        public int RepairTime = 1000;
        /// <summary>
        /// if omitted , default to vanilla repair cost.
        /// </summary>
        public ThingDef RepairCost = CBEXCompLikeThingDefOf.ComponentIndustrial;

        public CompProperties_BreakdownableEx()
        {
            //First catch on null or failure repaircost, revert back to vanilla cost.
            if (RepairCost == null)
                RepairCost = CBEXCompLikeThingDefOf.ComponentIndustrial;
            this.compClass = typeof(Isekaiob.CompBreakdownableEx);
        }
        /// <summary>
        /// Inject Repair cost & Repair time to infocard.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry statDrawEntry in base.SpecialDisplayStats(req))
            {
                yield return statDrawEntry;
            }
            //IDK WTF IS THIS. CAN SOMEONE TELLME WHAT IS THIS? CAN THIS BE SAFELY DELETED?
            IEnumerator<StatDrawEntry> enumerator = null;
            //Repair cost
            yield return new StatDrawEntry(StatCategoryDefOf.Building,
                "IKOB_CBEX_RCT".Translate(),
                this.RepairCost.label,
                "IKOB_CBEX_REPAIR_WITH".Translate(), 11000,
                null,Gen.YieldSingle<Dialog_InfoCard.Hyperlink>(new Dialog_InfoCard.Hyperlink(RepairCost)), false, false);
            //Repair Time
            yield return new StatDrawEntry(StatCategoryDefOf.Building, "IKOB_CBEX_RT".Translate(),
                RepairTime.ToString(), "IKOB_CBEX_REPAIR_TIME".Translate(), 11001);
            yield break;
            yield break;
        }
    }
}