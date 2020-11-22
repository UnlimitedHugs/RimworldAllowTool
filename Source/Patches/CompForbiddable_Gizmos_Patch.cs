using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Adds right-click ans shift-click support to the Forbid toggle button that appears for selected items.
	/// Also allows replacement of the button icon with the classic AllowTool allow and forbid icons. 
	/// </summary>
	[HarmonyPatch(typeof(CompForbiddable), nameof(CompForbiddable.CompGetGizmosExtra))]
	internal static class CompForbiddable_Gizmos_Patch {
		private static readonly Gizmo[] resultArray = new Gizmo[1];
		
		[HarmonyPostfix]
		public static void InjectDesignatorFunctionality(ref IEnumerable<Gizmo> __result) {
			var toggle = CommandFromEnumerator(__result);
			if(toggle == null) return; // safety against mod shenanigans
			AllowThingToggleHandler.EnhanceStockAllowToggle(toggle);
			resultArray[0] = toggle;
			__result = resultArray; // return a new enumerable to ensure the gizmo grid uses the same toggle instance
		}

		private static Command_Toggle CommandFromEnumerator(IEnumerable<Gizmo> enumerator) {
			using (var gizmoEnumerator = enumerator.GetEnumerator()) {
				if (!gizmoEnumerator.MoveNext()) return null;
				return gizmoEnumerator.Current as Command_Toggle;
			}
		}
	}
}