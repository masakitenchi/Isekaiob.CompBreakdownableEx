using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Isekaiob
{
    /// <summary>
    /// Todo: Transpiler insertion should have better performance but first I need to know how to write a proper one.
    /// </summary>
    public static class PatchMain
    {
        /// <summary>
        /// performance is a great issue to face with...
        /// unless we can directly modify the source code to add a property for it, we may going to suffer great performance loss bcz we have to acquire the comp everytime.
        /// or to make a crazy cache and use it.
        /// </summary>
        [HarmonyPatch(typeof(RimWorld.Building_WorkTable))]
        [HarmonyPatch("UsableForBillsAfterFueling")]
        static class Patch_Building_Worktable
        {
            /// <summary>
            /// Cached instance to boost speed.
            /// I assume you wont have 100 worktable on map;
            /// </summary>
            private static Dictionary<Building_WorkTable, Tuple<CompPowerTrader,CompBreakdownableEx>> Cache;
            static bool Prefix(Building_WorkTable __instance,ref bool __result)
            {
                CompPowerTrader cpt;
                CompBreakdownableEx cbex;
                //First create cache if it doesnt exist yet.
                if (Cache == null)
                {
                    Cache = new Dictionary<Building_WorkTable, Tuple<CompPowerTrader, CompBreakdownableEx>>(100);
#if DEBUG
                    Verse.Log.Message("[IKOB]CBEX Cache Dict @ RimWorld.Building_WorkTable.UsableForBillsAfterFueling Created");
#endif
                }
                //We have a working cache, and we cached this request before.
                if (Cache.ContainsKey(__instance))
                {
                    cbex = Cache[__instance].Item2;
                    //Pass back to original.
                    if (cbex == null)
                    {
                        return true;
                    }
                    //Continue to our logic.
                    cpt = Cache[__instance].Item1;
                    __result = (__instance.CanWorkWithoutPower ||
                                (cpt != null && cpt.PowerOn) && !cbex.BrokenDown
                        );
                    return false;
                }
                cbex = __instance.GetComp<CompBreakdownableEx>();
                if (cbex == null)
                {
                    return true;
                }
                cpt = __instance.GetComp<CompPowerTrader>();
                __result = (__instance.CanWorkWithoutPower ||
                            (cpt != null && cpt.PowerOn) && !cbex.BrokenDown
                    );
                Cache.Add(__instance,Tuple.Create(cpt,cbex));
                return false;
            }
        }
        [HarmonyPatch(typeof(Verse.CompHeatPusherPowered))]
        [HarmonyPatch("ShouldPushHeatNow",MethodType.Getter)]
        static class Patch_CompHeatPusherPowered
        {
            /// <summary>
            /// Cache to sped up lookups, i assume you wont have a total numbers of 100 Powered heater on all opened maps, or do you?
            /// </summary>
            private static Dictionary<Verse.CompHeatPusherPowered,
                Tuple<CompPowerTrader, CompRefuelable, CompBreakdownableEx>> Cache;
            static bool Prefix(Verse.CompHeatPusherPowered __instance,ref bool __result)
            {
                CompBreakdownableEx cbex;
                CompPowerTrader cpt;
                CompRefuelable cr;
                //Made cache.
                if (Cache == null)
                {
                    Cache =
                        new Dictionary<CompHeatPusherPowered,
                            Tuple<CompPowerTrader, CompRefuelable, CompBreakdownableEx>>(100);
                    #if DEBUG
                    Verse.Log.Message("[IKOB]CBEX Cache Dict @ Verse.CompHeatPusherPowered.ShouldPushHeatNow Created");
                    #endif
                }

                if (Cache.ContainsKey(__instance))
                {
#if DEBUG
                    Verse.Log.Message("[IKOB]CBEX Patch 2 Found instance in cache.");
#endif
                    cbex = Cache[__instance].Item3;
#if DEBUG
                    Verse.Log.Message(string.Format("[IKOB]CBEX Patch 2 Exec on {0}",__instance.parent.Position));
#endif
                    cpt = Cache[__instance].Item1;
                    cr = Cache[__instance].Item2;
                    //Not sure this works through, might need another logging use prefix to debug trace.
                    CompHeatPusher ancestor_ahead = __instance as CompHeatPusher;
                    __result =
                        ancestor_ahead.ShouldPushHeatNow && FlickUtility.WantsToBeOn(__instance.parent)
                                                   && (cpt == null || cpt.PowerOn) &&
                                                   (cr == null || cr.HasFuel) && !cbex.BrokenDown;
                    return false;
                }
                //its not saved in cache,process this normally then move.
                cbex = __instance.parent.GetComp<CompBreakdownableEx>();
                // Ahead turn over to ensure only cache the comp we need.
                if (cbex == null)
                {
                    return true;
                }
                cpt = __instance.parent.GetComp<CompPowerTrader>();
                cr = __instance.parent.GetComp<CompRefuelable>();
                CompHeatPusher ancestor = __instance as CompHeatPusher;
                    __result =
                        ancestor.ShouldPushHeatNow && FlickUtility.WantsToBeOn(__instance.parent)
                                                                       && (cpt == null || cpt.PowerOn) &&
                                                                       (cr == null || cr.HasFuel) && !cbex.BrokenDown;
                    Cache.Add(__instance,Tuple.Create(cpt,cr,cbex));
                    return false;
                }
            }
        }

    }