using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using AllowTool.Context;
using Harmony;
using UnityEngine;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Hooks additional calls into the drawing and event processing of Command buttons.
	/// This allows to draw an overlay icon on certain designators and intercept their right-click and shift-click events.
	/// </summary>
	[HarmonyPatch(typeof(Command))]
	[HarmonyPatch("GizmoOnGUI")]
	[HarmonyPatch(new[] {typeof(Vector2), typeof(float)})]
	internal static class Command_GizmoOnGUI_Patch {
		private static bool overlayInjected;
		private static bool postfixInjected;

		[HarmonyPrepare]
		private static void PrePatch() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!overlayInjected || !postfixInjected) AllowToolController.Logger.Error("Command_GizmoOnGUI infix could not be applied.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawRightClickIcon(IEnumerable<CodeInstruction> instructions) {
			var expectedMethod = AccessTools.Method(typeof(Widgets), "DrawTextureFitted",
				new[] {typeof(Rect), typeof(Texture), typeof(float), typeof(Vector2), typeof(Rect), typeof(float), typeof(Material)});
			var checksPassed = false;
			overlayInjected = postfixInjected = false;
			if (expectedMethod == null) {
				AllowToolController.Logger.Error("Failed to reflect required method: " + Environment.StackTrace);
			} else {
				checksPassed = true;
			}
			foreach (var instruction in instructions) {
				if (checksPassed) {
					// right after the gizmo icon texture is drawn
					if (!overlayInjected && instruction.opcode == OpCodes.Call && expectedMethod.Equals(instruction.operand)) {
						// push this (Command) arg
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// push topLeft (Vector2) arg
						yield return new CodeInstruction(OpCodes.Ldarg_1);
						// call our delegate
						yield return new CodeInstruction(OpCodes.Call, ((Action<Command, Vector2>)DesignatorContextMenuController.DrawCommandOverlayIfNeeded).Method);
						overlayInjected = true;
					}
					if (!postfixInjected && instruction.opcode == OpCodes.Ret) {
						// return value is on the stack (GizmoResult)
						// push this (Command) arg
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// call our delegate to change it or keep it
						yield return new CodeInstruction(OpCodes.Call, ((Func<GizmoResult, Command, GizmoResult>)DoCustomGizmoInteractions).Method);
						postfixInjected = true;
					}
				}
				yield return instruction;
			}
		}

		private static GizmoResult DoCustomGizmoInteractions(GizmoResult result, Command command) {
			if (result.State == GizmoState.Interacted || result.State == GizmoState.OpenedFloatMenu) {
				var designator = DesignatorContextMenuController.TryResolveCommandToDesignator(command);
				if (designator != null && DesignatorContextMenuController.TryProcessDesignatorInput(designator)) {
					// return a blank interact event if we intercepted the input
					return new GizmoResult(GizmoState.Clear, result.InteractEvent);
				}
			}
			return result;
		}
	}
}