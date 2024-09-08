using LudeonTK;
using Verse;

namespace Isekaiob
{
    public static class CompBreakdownableExtended_DebugTools
    {
        [DebugAction("General","[IKOB]CBEX Breakdown",false,false,false,false,0,actionType = DebugActionType.ToolMap,allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void MadeCompBreakdownExBroke()
        {
            foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()))
            {
                CompBreakdownableEx compBreakdownable = thing.TryGetComp<CompBreakdownableEx>();
                if (compBreakdownable != null && !compBreakdownable.BrokenDown)
                {
                    compBreakdownable.DoBreakdown();
                }
            }
        }
        [DebugAction("General","[IKOB]CBEX Repair",false,false,false,false,0,actionType = DebugActionType.ToolMap,allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void MadeCompBreakdownExRepaired()
        {
            foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()))
            {
                CompBreakdownableEx compBreakdownable = thing.TryGetComp<CompBreakdownableEx>();
                if (compBreakdownable != null && compBreakdownable.BrokenDown)
                {
                    compBreakdownable.Notify_Repaired();
                }
            }
        }

    }
}