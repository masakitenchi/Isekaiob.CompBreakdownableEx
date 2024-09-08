using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Isekaiob
{
    /// <summary>
    /// Can we make some cache here?
    ///
    /// </summary>
    public class WorkGiver_FixBrokenDownBuildingEx : WorkGiver_Scanner
    {
        /*
         * These are keyed string defined in Data/Core
         */
        public static string NotInHomeAreaTrans;
        private static string NoComponentsToRepairTrans;
        public static string TargetIsToBeDeconstructedTrans;

        public WorkGiver_FixBrokenDownBuildingEx()
        {
#if DEBUG
            Verse.Log.Message("[IKOB]CBEX WorkGiver_FixBrokenDownBuildingEx ctor running!");
#endif
        }

        public static void ResetStaticData()
        {
            NotInHomeAreaTrans = "NotInHomeArea".Translate();
            NoComponentsToRepairTrans = "NoComponentsToRepair".Translate();
            TargetIsToBeDeconstructedTrans = "TargetIsToBeDeconstructedTrans".Translate();
        }
        public override ThingRequest PotentialWorkThingRequest
        {
            get => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
        }

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return (IEnumerable<Thing>)pawn.Map.GetComponent<BreakdownManagerEx>().brokenDownThings;
        }
        /// <summary>
        /// you cant throttle this thing otherwise jobs wont spawn and you have to manually command pawn to fix.
        /// </summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return pawn.Map.GetComponent<BreakdownManagerEx>().brokenDownThings.Count == 0;
        }
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

        /// <summary>
        /// you cant throttle this thing otherwise your pawn wont fix these buildings by themselves.
        /// </summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
#if DEBUG
            Verse.Log.Message("[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Running Stage 1");
#endif
            /*
             * Stage 1,make sure the thing required to give job to is a repairable building and is owned by the faction of the pawn trying to give job to.
             */
            if (!(t is Building building) || !building.def.building.repairable || t.Faction != pawn.Faction ||
                // IsBrokenDown is an Extension Static Class Which caused this issue.
                !t.IsBrokenDownEx() || t.IsForbidden(pawn))
                return false;

#if DEBUG
            Verse.Log.Message("[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Stage 2");
#endif

            /*
             * Stage 2 Specially handle player faction home area to cutout most jobs
             */
            if (pawn.Faction == Faction.OfPlayer && !pawn.Map.areaManager.Home[t.Position])
            {
                JobFailReason.Is(WorkGiver_FixBrokenDownBuildingEx.NotInHomeAreaTrans);
                return false;
            }

#if DEBUG
            Verse.Log.Message("[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Stage 3");
#endif
            /*
             * Stage 3,check is building going to be deconstructed or is on fire.
             */
            if (!pawn.CanReserve((LocalTargetInfo)(Thing)building, ignoreOtherReservations: forced) ||
                pawn.Map.designationManager.DesignationOn((Thing)building, DesignationDefOf.Deconstruct) != null ||
                building.IsBurning())
            {
                JobFailReason.Is(WorkGiver_FixBrokenDownBuildingEx.TargetIsToBeDeconstructedTrans);
                return false;
            }
            // Just for safety, Skip if Not our dish (BUT HOW DID IT PASSED ALL CONDITIONS BEFORE)
            if (building.TryGetComp<CompBreakdownableEx>() == null)
                return false;
#if DEBUG
            Verse.Log.Message("[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Now Accquiring Repair Cost");
#endif
            // Read Repair cost From Comps.
            ThingDef RepairCostComp = building.TryGetComp<CompBreakdownableEx>().RepairCost;
            //Although this shouldnt happen bcz we already defaulted null cost to CompIndustrial in CompProperties ctor,Just in case.
            if (RepairCostComp == null)
            {
                Verse.Log.Error(string.Format("Failed to Get Proper Repair Cost on {0} at Pos {1} on Map {2}, Resetting to CompIndustrial", building.Label,
                    building.Position, building.Map));
                RepairCostComp = CBEXCompLikeThingDefOf.ComponentIndustrial;
            }
#if DEBUG
            Verse.Log.Message(string.Format("[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Accquired Repair Cost => [{0}], Label is [{1}]",RepairCostComp.defName,RepairCostComp.label));
#endif
            if (this.FindClosetRepairCostThing(pawn,RepairCostComp) != null)
                return true;
            JobFailReason.Is(WorkGiver_FixBrokenDownBuildingEx.NoComponentsToRepairTrans);
#if DEBUG
            Verse.Log.Message(string.Format(
                "[IKOB]WorkGiver_FixBrokenDownBuildingEx HasJobOnThing Failed because no RCC [{0}] is found on current Map",RepairCostComp.defName));
#endif
            return false;
        }
        
        /// <summary>
        /// Try fire the job.
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="t"></param>
        /// <param name="forced"></param>
        /// <returns></returns>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
#if DEBUG
            Verse.Log.Message("[IKOB]WorkGiver_FixBrokenDownBuildingEx JobOnThing Running");
#endif
            ThingDef RepairCostComp = t.TryGetComp<CompBreakdownableEx>()?.RepairCost;
            //What ? How ? I defaulted Cost to CompIndustrial on CompProperties ctor, this should never happen!
            if (RepairCostComp is null)
            {
                Verse.Log.Error(string.Format(
                    "[IKOB]WorkGiver_FixBrokenDownBuildingEx Failed to Get Proper Repair Cost on {0} at Pos {1} on Map {2}", t.Label,
                    t.Position, t.Map));
            }
            //Another place in vanilla dll where its written fixed.
            Thing closestComponent = this.FindClosetRepairCostThing(pawn,RepairCostComp);
            Job job = JobMaker.MakeJob(Isekaiob.JobDefOf.FixBrokenDownBuildingEx, (LocalTargetInfo)t,
                (LocalTargetInfo)closestComponent);
            job.count = 1;
            return job;
        }
        /// <summary>
        /// Need to pass The Repair cost through
        /// </summary>
        /// <param name="p">Pawn</param>
        /// <param name="rpc">Repair Cost Thing</param>
        /// <returns></returns>
        private Thing FindClosetRepairCostThing(Pawn p, ThingDef rpc)
        {
            return GenClosest.ClosestThingReachable(p.Position, p.Map, ThingRequest.ForDef(rpc), PathEndMode.Touch,
                TraverseParms.For(p, p.NormalMaxDanger()),
                validator: (Predicate<Thing>)(x => !x.IsForbidden(p) && p.CanReserve((LocalTargetInfo)x)));
        }
    }
}