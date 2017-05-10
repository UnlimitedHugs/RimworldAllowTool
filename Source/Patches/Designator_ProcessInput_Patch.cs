using AllowTool.Context;
using Harmony;
using UnityEngine;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Intercepts right clicks on supported designators, ignores other interactions
	/// </summary>
	[HarmonyPatch(typeof(Designator))]
	[HarmonyPatch("ProcessInput")]
	internal class Designator_ProcessInput_Patch {
		[HarmonyPrefix]
		public static bool InterceptRightClicksOnSupportedDesignators(Designator __instance) {
			if (Event.current.button != 1) return true; // right click only
			return !DesignatorContextMenuController.TryProcessRightClickOnDesignator(__instance);
		}	 
	}
}