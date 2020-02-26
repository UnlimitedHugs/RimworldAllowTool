using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AllowTool.Context;
using HarmonyLib;
using HugsLib.Utils;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Applies an infix to intercept the connection of a reverse designator to a Command_Action instance.
	/// This allows to identify Command_Action buttons that trigger a reverse designator when clicked.
	/// </summary>
	[HarmonyPatch]
	internal static class InspectGizmoGrid_DrawInspectGizmoGridFor_Patch {
		private static Type gizmoGridType; 
		private static Type designatorReferencerType; 
		private static FieldInfo holderDesignatorField;
		private static FieldInfo commandGroupKeyField;
		private static bool patchApplied;

		[HarmonyTargetMethod]
		// ReSharper disable once UnusedParameter.Global
		public static MethodInfo TargetMethod(Harmony inst) {
			// get our target type
			gizmoGridType = GenTypes.GetTypeInAnyAssembly("InspectGizmoGrid", "Rimworld");
			var method = AccessTools.Method(gizmoGridType, "DrawInspectGizmoGridFor");
			if (gizmoGridType != null) {
				const string expectedDesignatorFieldName = "des";
				// get the nested type that stores the reference to the current designator inside the iterator
				designatorReferencerType = gizmoGridType.GetNestedTypes(HugsLibUtility.AllBindingFlags).FirstOrDefault(t => AccessTools.Field(t, expectedDesignatorFieldName) != null);
				if (designatorReferencerType != null) {
					// get the field that stores the current designator
					holderDesignatorField = AccessTools.Field(designatorReferencerType, expectedDesignatorFieldName);
				}
			}
			commandGroupKeyField = AccessTools.Field(typeof(Command), "groupKey");
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patchApplied) AllowToolController.Logger.Warning("InspectGizmoGrid.DrawInspectGizmoGridFor patch failed. Reverse designator context menus are disabled.");
			});
			// make sure we have all required references, fail patch otherwise
			if (method == null || designatorReferencerType == null || holderDesignatorField == null || commandGroupKeyField == null) {
				AllowToolController.Logger.Warning("Could not reflect a required type or field: "+Environment.StackTrace);
				return null;
			}
			return method;
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> RegisterReverseDesignatorCommandPair(IEnumerable<CodeInstruction> instructions) {
			var instructionsArr = instructions.ToArray();
			var currentDesignatorHolderIndex = -1;
			var currentCommandIndex = -1;
			var prechecksSuccess = false;
			patchApplied = false;
			// find indices of local vars. They could be hardcoded, but this method is more future-proof
			try {
				CodeInstruction prevInstruction = null;
				foreach (var instruction in instructionsArr) {
					if (prevInstruction != null) {
						if (prevInstruction.opcode == OpCodes.Newobj && AccessTools.Constructor(typeof (Command_Action)).Equals(prevInstruction.operand)) {
							currentCommandIndex = ((LocalBuilder)instruction.operand).LocalIndex;
						}
						if (prevInstruction.opcode == OpCodes.Newobj && AccessTools.Constructor(designatorReferencerType).Equals(prevInstruction.operand)) {
							currentDesignatorHolderIndex = ((LocalBuilder)instruction.operand).LocalIndex;
						}
					}
					prevInstruction = instruction;
				}
			} catch (Exception e) {
				AllowToolController.Logger.Warning("Exception during local vars identification: " + e);
			}
			if (currentCommandIndex < 0 || currentDesignatorHolderIndex < 0) {
				AllowToolController.Logger.Warning("Failed to identify local variables for patching: "+Environment.StackTrace);
			} else {
				prechecksSuccess = true;
			}
			// currentCommandIndex: 7
			// currentDesignatorHolderIndex: 10 
			foreach (var instruction in instructionsArr) {
				yield return instruction;
				if (prechecksSuccess) {
					// after the group key for the command is set
					if (instruction.opcode == OpCodes.Stfld && commandGroupKeyField.Equals(instruction.operand)) {
						// push reference to designator reference holder
						yield return new CodeInstruction(OpCodes.Ldloc_S, currentDesignatorHolderIndex);
						// push designator reference
						yield return new CodeInstruction(OpCodes.Ldfld, holderDesignatorField);
						// push command reference
						yield return new CodeInstruction(OpCodes.Ldloc, currentCommandIndex);
						// call our method
						yield return new CodeInstruction(OpCodes.Call, ((Action<Designator, Command_Action>)DesignatorContextMenuController.RegisterReverseDesignatorPair).Method);
						patchApplied = true;
					}
				}
			}

		}
	}
}