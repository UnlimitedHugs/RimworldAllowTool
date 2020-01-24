using Harmony;
using Verse;

namespace AllowTool.Patches {
	[HarmonyPatch(typeof(DesignationCategoryDef))]
	[HarmonyPatch("ResolveDesignators")]
	[HarmonyPriority(Priority.HigherThanNormal)]
	internal static class DesignationCategoryDef_ResolveDesignators_Patch {
		[HarmonyPostfix]
		public static void InjectAllowToolDesignators(DesignationCategoryDef __instance) {
			AllowToolController.Instance.InjectDuringResolveDesignators(__instance);
		}
	}
}