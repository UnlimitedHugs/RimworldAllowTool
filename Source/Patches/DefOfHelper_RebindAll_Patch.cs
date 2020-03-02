using HarmonyLib;
using RimWorld;

namespace AllowTool.Patches {
	/// <summary>
	/// Adds an entry point before implied defs are generated.
	/// This is needed to prevent our custom WorkTypeDef from showing up in the Work tab.
	/// Also, setting handles must be ready before <see cref="Verse.DesignationCategoryDef.ResolveDesignators"/>
	/// </summary>
	[HarmonyPatch(typeof(DefOfHelper), "RebindAllDefOfs")]
	internal static class DefOfHelper_RebindAll_Patch {
		[HarmonyPostfix]
		public static void HookBeforeImpliedDefsGeneration(bool earlyTryMode) {
			if (!earlyTryMode) return;
			AllowToolController.Instance.OnBeforeImpliedDefGeneration();
		}
	}
}