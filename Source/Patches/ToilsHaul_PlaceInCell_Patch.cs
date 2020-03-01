using HarmonyLib;
using Verse.AI;

namespace AllowTool.Patches {
	/// <summary>
	/// Clears the "Haul urgently" designation when a hauled thing is delivered to a stockpile.
	/// </summary>
	[HarmonyPatch(typeof(Toils_Haul))]
	[HarmonyPatch("PlaceHauledThingInCell")]
	[HarmonyPatch(new[]{typeof(TargetIndex), typeof(Toil), typeof(bool), typeof(bool)})]
	internal static class ToilsHaul_PlaceInCell_Patch {
		[HarmonyPostfix]
		public static void ClearHaulUrgently(Toil __result) {
			var originalInitAction = __result.initAction;
			__result.initAction = () => {
				var carriedThing = __result.actor.carryTracker.CarriedThing;
				if (carriedThing != null) {
					__result.actor.Map.designationManager.TryRemoveDesignationOn(carriedThing, AllowToolDefOf.HaulUrgentlyDesignation);
				}
				originalInitAction();
			}; 
		}
	}
}