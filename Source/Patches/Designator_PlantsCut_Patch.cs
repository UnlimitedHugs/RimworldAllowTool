using HarmonyLib;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Prevents the Anima tree from being designated when dragging the Cut plants tool
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsCut), "CanDesignateThing", typeof(Thing))]
	internal static class Designator_PlantsCut_Patch {
		[HarmonyPostfix]
		public static void PreventAnimaTreeMassDesignation(Thing t, ref AcceptanceReport __result) {
			__result = AnimaTreeMassDesignationFix.RejectAnimaTreeMassDesignation(t, __result);
		}
	}
}