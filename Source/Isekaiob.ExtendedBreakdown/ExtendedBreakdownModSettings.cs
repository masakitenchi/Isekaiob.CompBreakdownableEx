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
        public override void ExposeData()
        {
            Scribe_Values.Look(ref iThrottleCompBreakdownableEx,"iThrottleCompBreakdownableEx");
            base.ExposeData();
        }
    }
}