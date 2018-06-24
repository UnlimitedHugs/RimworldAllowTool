using Harmony;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Allows only fully grown trees to be designated for chopping when enabled in the context menu.
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsHarvestWood))]
	[HarmonyPatch("CanDesignateThing")]
	[HarmonyPatch(new[] { typeof(Thing) })]
	internal static class DesignatorChop_CanDesignateThing_Patch {
		[HarmonyPostfix]
		public static void RejectPartiallyGrown(Thing t, ref AcceptanceReport __result) {
			if (__result.Accepted && AllowToolController.Instance.ChopFullyGrownSetting.Value) {
				var plant = t as Plant;
				__result = plant != null && plant.LifeStage == PlantLifeStage.Mature;
			}
		}
	}
}