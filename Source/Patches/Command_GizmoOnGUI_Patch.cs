﻿using System;
using System.Collections.Generic;
using System.Reflection;
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
	[HarmonyPatch]
	internal static class Command_GizmoOnGUI_Patch {
		private static bool overlayInjected;

		[HarmonyPrepare]
		private static void PrePatch() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!overlayInjected) AllowToolController.Logger.Error("Command_GizmoOnGUI infix could not be applied.");
			});
		}

		[HarmonyTargetMethod]
		private static MethodBase TargetMethod() {
			return AccessTools.Method(typeof(Command), "GizmoOnGUIInt", new[] { typeof(Rect), typeof(bool) }) ??
				AccessTools.Method(typeof(Command), "GizmoOnGUI", new[] { typeof(Vector2), typeof(float) });
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> DrawRightClickIcon(IEnumerable<CodeInstruction> instructions, MethodBase method) {
			var expectedMethod = AccessTools.Method(typeof(Command), "DrawIcon",
				new[] {typeof(Rect), typeof(Material)});
			overlayInjected = false;
			if (expectedMethod == null) {
				AllowToolController.Logger.Error("Failed to reflect required method: " + Environment.StackTrace);
			}

			var firstParameterType = method.GetParameters()[0].ParameterType;

			foreach (var instruction in instructions) {
				// right after the gizmo icon texture is drawn
				if (expectedMethod != null && instruction.opcode == OpCodes.Callvirt && expectedMethod.Equals(instruction.operand)) {
					// push this (Command) arg
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					if (firstParameterType == typeof(Rect)) {
						// push butRect.min where butRect is Rect arg
						yield return new CodeInstruction(OpCodes.Ldarga, (short)1);
						yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.min)));
					} else {
						// push topLeft (Vector2) arg
						yield return new CodeInstruction(OpCodes.Ldarg_1);
					}
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