using HarmonyLib;
using Verse;

namespace AllowTool.Patches {
	[HarmonyPatch(typeof(DesignationCategoryDef))]
	[HarmonyPatch("ResolveDesignators")]
	[HarmonyPriority(Priority.LowerThanNormal)]
	internal static class DesignationCategoryDef_ResolveDesignators_Patch {
		[HarmonyPostfix]
		public static void InjectAllowToolDesignators() {
			AllowToolController.Instance.OnDesignationCategoryResolveDesignators();
		}
	}
}