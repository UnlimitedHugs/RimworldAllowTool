using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Prevent the Cut designator from displaying a "Chop" icon when trees are selected.
	/// We compensate for this by injecting a Designator_PlantsHarvestWood in ReverseDesignatorHandler.
	/// This fix allows us to pick the proper tool when shift-clicking the designator, 
	/// and get the correct context menu options when right-clicking.
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsCut))]
	[HarmonyPatch("IconReverseDesignating")]
	internal static class DesignatorCut_ReverseIcon_Patch {
		[HarmonyPrefix]
		public static bool NeverChangeIcon(Designator __instance, out Texture2D __result, out float angle, out Vector2 offset) {
			__result = __instance.icon;
			offset = default(Vector2);
			angle = 0f;
			return false;
		}
	}

	/// <summary>
	/// Prevent the Cut designator from displaying a "Chop" label when trees are selected
	/// </summary>
	[HarmonyPatch(typeof(Designator_PlantsCut))]
	[HarmonyPatch("LabelCapReverseDesignating")]
	internal static class DesignatorCut_ReverseLabel_Patch {
		[HarmonyPrefix]
		public static bool NeverChangeLabel(Designator __instance, out string __result) {
			__result = __instance.LabelCap;
			return false;
		}
	}
}