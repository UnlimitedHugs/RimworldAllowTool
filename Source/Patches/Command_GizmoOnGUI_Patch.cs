using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using AllowTool.Context;
using Harmony;
using UnityEngine;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Hooks an additional call right after the Command icon texture is drawn.
	/// This allows to draw an overlay icon on certain designators.
	/// </summary>
	[HarmonyPatch(typeof(Command))]
	[HarmonyPatch("GizmoOnGUI")]
	[HarmonyPatch(new []{typeof(Vector2)})]
	internal static class Command_GizmoOnGUI_Patch {
		private static bool injectCompleted;

		[HarmonyPrepare]
		private static void PrePatch() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!injectCompleted) AllowToolController.Logger.Warning("Command_GizmoOnGUI infix could not be applied. Desginator button overlays disabled.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawRightClickIcon(IEnumerable<CodeInstruction> instructions) {
			var expectedMethod = AccessTools.Method(typeof (Widgets), "DrawTextureFitted", new[] {typeof (Rect), typeof (Texture), typeof (float), typeof (Vector2), typeof (Rect), typeof (float)});
			var checksPassed = false;
			if (expectedMethod == null) {
				AllowToolController.Logger.Error("Failed to reflect required method: " + Environment.StackTrace);
			} else {
				checksPassed = true;
			}
			foreach (var instruction in instructions) {
				if (checksPassed && !injectCompleted) {
					// right after the gizmo icon texture is drawn
					if (instruction.opcode == OpCodes.Call && expectedMethod.Equals(instruction.operand)) {
						// push this (Command) arg
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// push topLeft (Vector2) arg
						yield return new CodeInstruction(OpCodes.Ldarg_1);
						// call our delegate
						yield return new CodeInstruction(OpCodes.Call, ((Action<Command, Vector2>)DesignatorContextMenuController.DrawCommandOverlayIfNeeded).Method);
						injectCompleted = true;
					}
				}
				yield return instruction;
			}
		}
	}
}