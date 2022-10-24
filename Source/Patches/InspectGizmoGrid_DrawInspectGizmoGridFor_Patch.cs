using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AllowTool.Context;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Clears the pairs in DesignatorContextMenuController when the gizmo cache in InspectGizmoGrid is cleared.
	/// </summary>
	[HarmonyPatch(typeof(InspectGizmoGrid), "DrawInspectGizmoGridFor")]
	internal static class InspectGizmoGrid_DrawInspectGizmoGridFor_Patch {
		private static bool patchApplied;

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> ClearReverseDesignators(IEnumerable<CodeInstruction> instructions) {
			var gizmoListField = AccessTools.Field(typeof(InspectGizmoGrid), "gizmoList");
			var clearListMethod = AccessTools.Method(typeof(List<Gizmo>), "Clear");

			if (gizmoListField == null || gizmoListField.FieldType != typeof(List<Gizmo>))
				throw new Exception("Failed to reflect InspectGizmoGrid.gizmoList");
			if (clearListMethod == null) throw new Exception("Failed to reflect List.Clear");

			var instructionsArr = instructions.ToArray();
			patchApplied = false;

			CodeInstruction prevInstruction = null;
			foreach (var instruction in instructionsArr) {
				yield return instruction;

				// look for the call that clears the gizmoList
				if (prevInstruction != null
					&& prevInstruction.LoadsField(gizmoListField)
					&& instruction.Calls(clearListMethod)) {
					// insert our method after that
					yield return new CodeInstruction(OpCodes.Call,
						((Action)DesignatorContextMenuController.ClearReverseDesignatorPairs).Method);
					patchApplied = true;
				}

				prevInstruction = instruction;
			}

			if (!patchApplied) {
				AllowToolController.Logger.Warning("Failed to transpile method DrawInspectGizmoGridFor");
			}
		}
	}
}