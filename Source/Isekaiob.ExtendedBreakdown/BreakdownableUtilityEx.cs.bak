using Verse;

namespace Isekaiob
{
    public static class BreakdownableUtilityEx
    {
        public static bool IsBrokenDownEx(this Thing t)
        {
            CompBreakdownableEx comp = t.TryGetComp<CompBreakdownableEx>();
            return comp != null && comp.BrokenDown;
        }
    }
}