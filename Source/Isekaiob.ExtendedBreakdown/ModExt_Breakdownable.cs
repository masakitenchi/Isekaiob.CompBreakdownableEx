using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using static HarmonyLib.Code;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;

namespace Isekaiob
{
	[StaticConstructorOnStartup]
	public class ModExtension_Breakdownable : DefModExtension
	{
		public string label = "BrokenDown";
		public int eventMTBInTicks = CompBreakdownable.BreakdownMTBTicks;
		public ThingDef RepairCost;
		public int ticksToRepair = 1000;

		static ModExtension_Breakdownable()
		{
			LongEventHandler.ExecuteWhenFinished(() => 
			{
				foreach(var def in DefDatabase<ThingDef>.AllDefs.Where(x => x.HasModExtension<ModExtension_Breakdownable>()))
				{
					if (def.GetModExtension<ModExtension_Breakdownable>() is ModExtension_Breakdownable modExt && modExt.RepairCost == null)
					{
						Log.Message($"[CBEX] {def.defName} has no repair cost, defaulting to ComponentIndustrial");
						modExt.RepairCost = ThingDefOf.ComponentIndustrial;
					}
				}
			});
		}
	}

	[HarmonyPatch]
	public static class ModExt_Breakdownable
	{
		#region eventMTBInTicks
		private static readonly FieldInfo parent = AccessTools.Field(typeof(CompBreakdownable), nameof(CompBreakdownable.parent));
		private static readonly FieldInfo def = AccessTools.Field(typeof(Thing), nameof(Thing.def));
		private static readonly FieldInfo ticks = AccessTools.Field(typeof(ModExtension_Breakdownable), nameof(ModExtension_Breakdownable.eventMTBInTicks));
		private static readonly MethodInfo get_ModExt = AccessTools.Method(typeof(Def), nameof(Def.GetModExtension), generics: new[] { typeof(ModExtension_Breakdownable) });


		[HarmonyPatch(typeof(CompBreakdownable), nameof(CompBreakdownable.CheckForBreakdown))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> MTBRedirect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> inst = instructions.ToList();
			int tickIndex = inst.FindIndex(i => i.Is(OpCodes.Ldc_R4, CompBreakdownable.BreakdownMTBTicks));
			if (tickIndex == -1)
			{
				Log.Error("Failed to find MTB in CheckForBreakdown");
				return inst;
			}
			Label arg1 = generator.DefineLabel();
			inst[tickIndex].labels.Add(arg1);
			Label arg2 = generator.DefineLabel();
			inst[tickIndex + 1].labels.Add(arg2);
			inst.InsertRange(tickIndex, new CodeInstruction[]
			{
				// Rand.MTBEventOccurs(parent.def.GetModExtension<ModExtension_Breakdownable>()?.eventMTBInTicks ?? 13680000f, 1f, 1041f);
				Ldarg_0,
				Ldfld[parent],
				Ldfld[def],
				Callvirt[get_ModExt],
				Brfalse[arg1],
				Ldarg_0,
				Ldfld[parent],
				Ldfld[def],
				Callvirt[get_ModExt],
				Ldfld[ticks],
				Conv_R4,
				Br_S[arg2]
			});
			return inst;
		}
		#endregion

		#region RepairCost
		private static readonly MethodInfo FindClosestComponent = AccessTools.Method(typeof(WorkGiver_FixBrokenDownBuilding), nameof(WorkGiver_FixBrokenDownBuilding.FindClosestComponent));
		private static readonly FieldInfo NoComponentString = AccessTools.Field(typeof(WorkGiver_FixBrokenDownBuilding), nameof(WorkGiver_FixBrokenDownBuilding.NoComponentsToRepairTrans));
		private static readonly FieldInfo label = AccessTools.Field(typeof(Def), nameof(Def.label));

		[HarmonyPatch(typeof(WorkGiver_FixBrokenDownBuilding), nameof(WorkGiver_FixBrokenDownBuilding.HasJobOnThing))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ComponentRedirect(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			/*
			// if (FindClosestComponent(pawn) == null)
			IL_00b4: ldarg.0
			IL_00b5: ldarg.1
								/
			ldloc.0				Insert here
								\
			Replace
				IL_00b6: call instance class Verse.Thing RimWorld.WorkGiver_FixBrokenDownBuilding::FindClosestComponent(class Verse.Pawn)
			with
				call class Verse.Thing Isekaiob.ModExt_Breakdownable::FindClosestComponentReroute(this WorkGiver_FixBrokenDownBuilding, class Verse.Pawn, class Verse.Building)
			IL_00bb: brtrue.s IL_00ca
			*/
			LocalBuilder modExtension = generator.DeclareLocal(typeof(ModExtension_Breakdownable));
			foreach (var inst in instructions)
			{
				if (inst.Is(OpCodes.Call, FindClosestComponent))
				{
					yield return Ldarg_2;
					inst.operand = AccessTools.Method(typeof(ModExt_Breakdownable), nameof(FindClosestComponentReroute));
					yield return inst;
					continue;
				}
				if (inst.Is(OpCodes.Ldsfld, NoComponentString))
				{
					Label translationLabel = generator.DefineLabel();
					Label componentLabel = generator.DefineLabel();
					yield return Ldstr["Isekaiob.NoComponentsToRepairTrans"];
					yield return Ldloc[modExtension];
					yield return Brfalse_S[componentLabel];
					yield return Ldloc[modExtension];
					yield return Ldfld[AccessTools.Field(typeof(ModExtension_Breakdownable), nameof(ModExtension_Breakdownable.RepairCost))];
					yield return Br_S[translationLabel];
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ThingDefOf), nameof(ThingDefOf.ComponentIndustrial))) { labels = new List<Label> { componentLabel } };
					yield return new CodeInstruction(OpCodes.Ldfld, label) { labels = new List<Label> { translationLabel } };
					yield return Call[AccessTools.Method(typeof(NamedArgument), "op_Implicit", new[] { typeof(string) })];
					yield return Call[AccessTools.Method(typeof(TranslatorFormattedStringExtensions), nameof(TranslatorFormattedStringExtensions.Translate), parameters: new[] { typeof(string), typeof(NamedArgument) })];
					// TaggedString has a overriden version of ToString, yet using that will raise an error
					yield return Call[AccessTools.Method(typeof(TaggedString), "op_Implicit", parameters: new[] { typeof(TaggedString) })];
					continue;
				}
				yield return inst;
			}
		}

		[HarmonyPatch(typeof(WorkGiver_FixBrokenDownBuilding), nameof(WorkGiver_FixBrokenDownBuilding.JobOnThing))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> RerouteThing(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			bool found = false;
			LocalBuilder modExtension = generator.DeclareLocal(typeof(ModExtension_Breakdownable));
			foreach(var inst in instructions)
			{
				if (!found && inst.Is(OpCodes.Call, FindClosestComponent))
				{
					found = true;
					yield return Ldarg_2;
					inst.operand = AccessTools.Method(typeof(ModExt_Breakdownable), nameof(FindClosestComponentReroute));
					yield return inst;
					continue;
				}
				yield return inst;
			}
		}

		// You can add 'this' to WorkGiver_FixBrokenDownBuilding, but it's redundant in CIL
		private static Thing FindClosestComponentReroute(WorkGiver_FixBrokenDownBuilding instance, Pawn pawn, Thing building)
		{
			ThingDef def = building.def.GetModExtension<ModExtension_Breakdownable>()?.RepairCost ?? ThingDefOf.ComponentIndustrial;
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
		}
		#endregion

		#region ticksToRepair
		public static IEnumerable<CodeInstruction> RepairTickReroute(IEnumerable<CodeInstruction> instructions)
		{
			// Find ldfld class RimWorld.JobDriver_FixBrokenDownBuilding RimWorld.JobDriver_FixBrokenDownBuilding/'<MakeNewToils>d__8'::'<>4__this' and save the index
			List<CodeInstruction> inst = instructions.ToList();
			int thisIndex = inst.FindIndex(i => i.Is(OpCodes.Ldfld, AccessTools.Field(AccessTools.Inner(typeof(JobDriver_FixBrokenDownBuilding), "<MakeNewToils>d__8"), "<>4__this")));
			if (thisIndex == -1)
			{
				Log.Error("Failed to find state machine in JobDriver_FixBrokenDownBuilding");
				return inst;
			}
			int tickIndex = inst.FindIndex(i => i.Is(OpCodes.Ldc_I4, 1000));
			if (tickIndex == -1)
			{
				Log.Error("Failed to find ticks in JobDriver_FixBrokenDownBuilding");
				return inst;
			}
			inst[tickIndex] = new CodeInstruction(OpCodes.Ldloc_1); // <MakeNewToils>d__8.<>4__this
			inst.Insert(tickIndex + 1, Call[AccessTools.Method(typeof(ModExt_Breakdownable), nameof(GetFixingTicks))]);
			return inst;
		}

		private static int GetFixingTicks(JobDriver_FixBrokenDownBuilding instance)
		{
			return instance.Building.def.GetModExtension<ModExtension_Breakdownable>()?.ticksToRepair ?? 1000;
		}
		#endregion

		#region ExtraToolTips
		[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
		[HarmonyPostfix]
		public static IEnumerable<StatDrawEntry> ShowComponentAndTime(IEnumerable<StatDrawEntry> result, ThingDef __instance)
		{
			foreach (var entry in result) yield return entry;
			if (__instance.GetModExtension<ModExtension_Breakdownable>() is ModExtension_Breakdownable modExt)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.Building, (string)"IKOB_CBEX_RepairCostRequirements".Translate(), modExt.RepairCost.LabelCap, "IKOB_CBEX_REPAIR_WITH".Translate(), 200, hyperlinks: new Dialog_InfoCard.Hyperlink[]
				{
					new Dialog_InfoCard.Hyperlink(modExt.RepairCost)
				});
				yield return new StatDrawEntry(StatCategoryDefOf.Building, (string)"IKOB_CBEX_RT".Translate(), modExt.ticksToRepair.ToString(), "IKOB_CBEX_REPAIR_TIME".Translate(), 201);
			}
		}
		#endregion

		#region InspectString
		[HarmonyPatch(typeof(CompBreakdownable), nameof(CompBreakdownable.CompInspectStringExtra))]
		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ReplaceInspectString(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			List<CodeInstruction> inst = instructions.ToList();
			for (int i = 0; i < inst.Count; i++)
			{
				if (inst[i].opcode == OpCodes.Ldstr && (string)inst[i].operand == "BrokenDown")
				{
					Label translateCall = generator.DefineLabel();
					inst[i+1].labels.Add(translateCall);
					Label defLabel = generator.DefineLabel();
					// this.def.GetModExtension<ModExtension_Breakdownable>()
					yield return Ldarg_0;
					yield return Ldfld[parent];
					yield return Ldfld[def];
					yield return Callvirt[get_ModExt];
					yield return Brfalse_S[defLabel];
					yield return Ldarg_0;
					yield return Ldfld[parent];
					yield return Ldfld[def];
					yield return Callvirt[get_ModExt];
					yield return Ldfld[AccessTools.Field(typeof(ModExtension_Breakdownable), nameof(ModExtension_Breakdownable.label))];
					yield return Br_S[translateCall];
					yield return new CodeInstruction(OpCodes.Ldstr, "BrokenDown") { labels = new List<Label> { defLabel } };
					continue;
				}
				yield return inst[i];
			}
		}
		#endregion
	}
}

