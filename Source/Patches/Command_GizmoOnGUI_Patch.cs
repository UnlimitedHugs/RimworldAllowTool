using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using AllowTool.Context;
using HarmonyLib;
using HugsLib.Utils;
using RimWorld;
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

		[HarmonyPrepare]
		private static void PrePatch() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!overlayInjected) AllowToolController.Logger.Error("Command_GizmoOnGUI infix could not be applied.");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawRightClickIcon(IEnumerable<CodeInstruction> instructions) {
			var expectedMethod = AccessTools.Method(typeof(Command), "DrawIcon",
				new[] {typeof(Rect), typeof(Material)});
			overlayInjected = false;
			if (expectedMethod == null) {
				AllowToolController.Logger.Error("Failed to reflect required method: " + Environment.StackTrace);
			}

			foreach (var instruction in instructions) {
				// right after the gizmo icon texture is drawn
				if (expectedMethod != null && instruction.opcode == OpCodes.Callvirt && expectedMethod.Equals(instruction.operand)) {
					// push this (Command) arg
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					// push topLeft (Vector2) arg
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					// call our delegate
					yield return new CodeInstruction(OpCodes.Call, ((Action<Command, Vector2>)DesignatorContextMenuController.DrawCommandOverlayIfNeeded).Method);
					overlayInjected = true;
				}

				yield return instruction;
			}
		}

		[HarmonyPostfix]
		public static void InterceptInteraction(ref GizmoResult __result, Command __instance) {
			if (__result.State == GizmoState.Interacted || __result.State == GizmoState.OpenedFloatMenu) {
				var designator = DesignatorContextMenuController.TryResolveCommandToDesignator(__instance);
				if (designator != null && DesignatorContextMenuController.TryProcessDesignatorInput(designator)) {
					// return a blank interact event if we intercepted the input
					__result = new GizmoResult(GizmoState.Clear, __result.InteractEvent);
				}
			}
		}
	}
}