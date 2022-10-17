using AllowTool.Context;
using HarmonyLib;
using Verse;

namespace AllowTool.Patches {
	
	/// <summary>
	/// Applies an infix to intercept the connection of a reverse designator to a Command_Action instance.
	/// This allows to identify Command_Action buttons that trigger a reverse designator when clicked.
	/// </summary>
	[HarmonyPatch(typeof(Designator), "CreateReverseDesignationGizmo")]
	internal static class Designator_CreateReverseDesignationGizmo_Patch {

		[HarmonyPostfix]
		internal static void CreateReverseDesignationGizmo_Postfix(Designator __instance, Command_Action __result)
		{
			if (__result == null) return;
			DesignatorContextMenuController.RegisterReverseDesignatorPair(__instance, __result);
		}
	}
}