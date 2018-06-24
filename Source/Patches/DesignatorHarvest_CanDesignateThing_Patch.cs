using Harmony;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Allows only fully grown plants to be designated for harvesting when enabled in the context menu.
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsHarvest))]
	[HarmonyPatch("CanDesignateThing")]
	[HarmonyPatch(new[] { typeof(Thing) })]
	internal static class DesignatorHarvest_CanDesignateThing_Patch {
		[HarmonyPostfix]
		public static void RejectPartiallyGrown(Thing t, ref AcceptanceReport __result) {
			if (__result.Accepted && AllowToolController.Instance.HarvestFullyGrownSetting.Value) {
				var plant = t as Plant;
				__result = plant != null && plant.LifeStage == PlantLifeStage.Mature;
			}
		}
	}
}