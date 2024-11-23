using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Isekaiob
{

    public class CompBreakdownableExtended_Mod : Mod
    {
        public static ExtendedBreakdownModSettings settings;
        internal static Harmony hInstance;

        /// <summary>
        /// Todo:Init harmony patch here
        /// </summary>
        /// <param name="content"></param>
        public CompBreakdownableExtended_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ExtendedBreakdownModSettings>();
            hInstance ??= new Harmony("isekaiob.cbex");
            hInstance.PatchAll(Assembly.GetExecutingAssembly());
            MethodInfo moveNext = AccessTools.Method(AccessTools.Inner(typeof(RimWorld.JobDriver_FixBrokenDownBuilding), "<MakeNewToils>d__8"), "MoveNext");
			if (moveNext is not null)
			{
				hInstance.Patch(moveNext, transpiler: new HarmonyMethod(typeof(ModExt_Breakdownable), nameof(ModExt_Breakdownable.RepairTickReroute)));
			}
#if DEBUG
            foreach (var patchedMethod in hInstance.GetPatchedMethods())
            {
                Verse.Log.Message(string.Format("[IKOB]CBEX Harmony Patched Method {0}",patchedMethod.Name));
            }
#endif
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {

            Listing_Standard lst = new Listing_Standard();
            lst.Begin(inRect);
            lst.Label("iThrottleCompBreakdownableEx");
            lst.Label(settings.iThrottleCompBreakdownableEx.ToString());
            lst.Label("iThrottleCompBreakdownableEx_Desc".Translate());
            lst.IntAdjuster(ref settings.iThrottleCompBreakdownableEx, 100, 641);
            lst.CheckboxLabeled("bNonPoweredWorktablesBreakdown".Translate(), ref settings.bNonPoweredWorktablesBreakdown);
            lst.GapLine();
            if (lst.ButtonText("Reset Settings".Translate()))
            {
                settings.iThrottleCompBreakdownableEx = 1041;
                settings.bNonPoweredWorktablesBreakdown = true;
            }
            lst.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "[IKOB]CompBreakdownableExtended".Translate();
        }
    }
}