using HarmonyLib;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Prevents the Anima and Gauranlen trees from being designated when dragging the Chop wood tool
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsHarvestWood), "CanDesignateThing", typeof(Thing))]
	internal static class Designator_PlantsHarvestWood_Patch {
		[HarmonyPostfix]
		public static void PreventSpecialTreeMassDesignation(Thing t, ref AcceptanceReport __result) {
			__result = SpecialTreeMassDesignationFix.RejectSpecialTreeMassDesignation(t, __result);
		}
	}
}