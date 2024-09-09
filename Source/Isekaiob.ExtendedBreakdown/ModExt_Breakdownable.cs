using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using static HarmonyLib.Code;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Mono.Posix;

namespace Isekaiob
{
	public class ModExtension_Breakdownable : DefModExtension
	{
		public int eventMTBInTicks = CompBreakdownable.BreakdownMTBTicks;
		public ThingDef RepairCost = ThingDefOf.ComponentIndustrial;
		public int ticksToRepair = 1000;
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
		[HarmonyDebug]
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
					Label instLabel = generator.DefineLabel();
					inst.labels.Add(instLabel);
					Label ComponentIndustrial = generator.DefineLabel();
					yield return Ldloc_0;
					yield return Ldfld[def];
					yield return Callvirt[get_ModExt];
					yield return Stloc[modExtension];
					yield return Ldloc[modExtension];
					yield return Brfalse_S[ComponentIndustrial];
					yield return Ldloc[modExtension];
					yield return Ldfld[AccessTools.Field(typeof(ModExtension_Breakdownable), nameof(ModExtension_Breakdownable.RepairCost))];
					yield return Br_S[instLabel];
					yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(ThingDefOf), nameof(ThingDefOf.ComponentIndustrial))) { labels = new List<Label> { ComponentIndustrial } };
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
					yield return Call[AccessTools.Method(typeof(TaggedString), "op_Implicit", parameters: new[] { typeof(TaggedString)})];
					continue;
				}
				yield return inst;
			}
		}

		// You can add 'this' to WorkGiver_FixBrokenDownBuilding, but it's redundant in CIL
		private static Thing FindClosestComponentReroute(WorkGiver_FixBrokenDownBuilding instance, Pawn pawn, ThingDef def)
		{
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.InteractionCell, TraverseParms.For(pawn, pawn.NormalMaxDanger()), 9999f, (Thing x) => !x.IsForbidden(pawn) && pawn.CanReserve(x));
		}
		#endregion

		#region ticksToRepair
		[HarmonyPatch(typeof(JobDriver_FixBrokenDownBuilding), nameof(JobDriver_FixBrokenDownBuilding.MakeNewToils))]
		[HarmonyPostfix]
		public static IEnumerable<Toil> RepairTickReroute(IEnumerable<Toil> result)
		{
			foreach (var toil in result)
			{
				if (toil.debugName == "Wait")
				{
					toil.defaultDuration = toil.actor.jobs.curJob.targetA.Thing.def.GetModExtension<ModExtension_Breakdownable>()?.ticksToRepair ?? 1000;
				}
				yield return toil;
			}
		}
		#endregion

		#region ExtraToolTips
		[HarmonyPatch(typeof(ThingDef), nameof(ThingDef.SpecialDisplayStats))]
		[HarmonyPostfix]
		public static IEnumerable<StatDrawEntry> ShowComponentAndTime(IEnumerable<StatDrawEntry> result, ThingDef __instance)
		{
			foreach(var entry in result) yield return entry;
			if (__instance.GetModExtension<ModExtension_Breakdownable>() is ModExtension_Breakdownable modExt)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.Building, (string)"IKOB_CBEX_RepairCostRequirements".Translate(), modExt.RepairCost.LabelCap, "IKOB_CBEX_REPAIR_WITH".Translate(), 200);
				yield return new StatDrawEntry(StatCategoryDefOf.Building, (string)"IKOB_CBEX_RT".Translate(), modExt.ticksToRepair.ToString(), "IKOB_CBEX_REPAIR_TIME".Translate(), 201);
			}
		}
		#endregion
	}
}

