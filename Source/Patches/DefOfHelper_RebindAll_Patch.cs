using Harmony;
using RimWorld;

namespace AllowTool.Patches {
	/// <summary>
	/// Adds an entry point before implied defs are generated.
	/// This is needed to prevent our custom WorkTypeDef from showing up in the Work tab.
	/// </summary>
	[HarmonyPatch(typeof(DefOfHelper), "RebindAllDefOfs")]
	internal static class DefOfHelper_RebindAll_Patch {
		[HarmonyPostfix]
		public static void HookBeforeImpliedDefsGeneration() {
			AllowToolController.HideHaulUrgentlyWorkTypeIfDisabled();
		}
	}
}