using Verse;

namespace Isekaiob
{
    /// <summary>
    /// Note: Throttle could be broken when used with Performance optimizer.
    /// </summary>
    public class ExtendedBreakdownModSettings:ModSettings
    {
        /// <summary>
        /// Sleep interval used in vanilla BreakdownManager.
        /// Made to a changeable parameter here.
        /// </summary>
        public int iThrottleCompBreakdownableEx = 1041;
        /// <summary>
        /// Should non-powered worktables be able to be broken down
        /// </summary>
        public bool bNonPoweredWorktablesBreakdown = true;
        public override void ExposeData()
        {
            Scribe_Values.Look(ref iThrottleCompBreakdownableEx,"iThrottleCompBreakdownableEx");
            Scribe_Values.Look(ref bNonPoweredWorktablesBreakdown,"bNonPoweredWorktablesBreakdown");
            base.ExposeData();
        }
    }
}