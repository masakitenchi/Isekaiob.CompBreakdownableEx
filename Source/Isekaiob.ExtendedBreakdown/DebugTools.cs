using LudeonTK;
using Verse;
using RimWorld;
using System.Linq;

namespace Isekaiob
{
    public static class CompBreakdownableExtended_DebugTools
    {
        [DebugAction("General","[IKOB]CBEX Breakdown",false,false,false,false,0,actionType = DebugActionType.ToolMap,allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void MadeCompBreakdownExBroke()
        {
            foreach (Thing thing in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()))
            {
                CompBreakdownable compBreakdownable = thing.TryGetComp<CompBreakdownable>();
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
                CompBreakdownable compBreakdownable = thing.TryGetComp<CompBreakdownable>();
                if (compBreakdownable != null && compBreakdownable.BrokenDown)
                {
                    compBreakdownable.Notify_Repaired();
                }
            }
        }
        [DebugAction("General","[IKOB]CBEX Log all cost",actionType = DebugActionType.Action,allowedGameStates = AllowedGameStates.Playing)]
        public static void LogAllRepairCost()
        {
            TableDataGetter<ThingDef> label = new TableDataGetter<ThingDef>("Name", (ThingDef x) => x.LabelCap);
            TableDataGetter<ThingDef> cost = new TableDataGetter<ThingDef>("Cost", (ThingDef x) => x.GetModExtension<ModExtension_Breakdownable>().RepairCost.LabelCap);
            DebugTables.MakeTablesDialog(DefDatabase<ThingDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_Breakdownable>()), label, cost);
        }

    }
}