using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
namespace Isekaiob
{
    public class JobDriver_FixBrokenDownBuildingEx : JobDriver
    {
        private const TargetIndex BuildingInd = TargetIndex.A;
        private const TargetIndex ComponentInd = TargetIndex.B;
        private const int TicksDuration = 1000;

        private Building Building => (Building) this.job.GetTarget(TargetIndex.A).Thing;

        private Thing Components => this.job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve((LocalTargetInfo) (Thing) this.Building, this.job, errorOnFailed: errorOnFailed) && this.pawn.Reserve((LocalTargetInfo) this.Components, this.job, errorOnFailed: errorOnFailed);
        }

        /// <summary>
        /// The most tricky one here. Jetbrain dotPeek doesnt work, thanks dnspy.
        /// </summary>
        /// <returns>WTF why cant I read this is this actually code</returns>
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, false, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            int TicksToRepair = this.Building.GetComp<CompBreakdownableEx>().RepairTime;
            Toil toil = Toils_General.Wait(TicksToRepair,TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            toil.WithEffect(this.Building.def.repairEffect, TargetIndex.A, null);
            toil.WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
            toil.activeSkill = (() => SkillDefOf.Construction);
            yield return toil;
            Toil toil2 = ToilMaker.MakeToil("MakeNewToils");
            toil2.initAction = delegate()
            {
                this.Components.Destroy(DestroyMode.Vanish);
                if (Rand.Value > this.pawn.GetStatValue(StatDefOf.FixBrokenDownBuildingSuccessChance, true, -1))
                {
                    MoteMaker.ThrowText((this.pawn.DrawPos + this.Building.DrawPos) / 2f, base.Map, "TextMote_FixBrokenDownBuildingFail".Translate(), 3.65f);
                    return;
                }
                this.Building.GetComp<CompBreakdownableEx>().Notify_Repaired();
            };
            yield return toil2;
            yield break;
        }
    }
}