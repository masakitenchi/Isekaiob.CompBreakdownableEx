using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Isekaiob
{

    public class CompBreakdownableExtended_Mod : Mod
    {
        public static ExtendedBreakdownModSettings settings;

        /// <summary>
        /// Todo:Init harmony patch here
        /// </summary>
        /// <param name="content"></param>
        public CompBreakdownableExtended_Mod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ExtendedBreakdownModSettings>();
            Harmony hInstance = new Harmony("isekaiob.cbex");
            hInstance.PatchAll(Assembly.GetExecutingAssembly());
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