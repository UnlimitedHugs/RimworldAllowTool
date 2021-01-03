using HarmonyLib;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Inject our own reverse designators when the vanilla ones are initialized
	/// </summary>
	[HarmonyPatch(typeof(ReverseDesignatorDatabase), "InitDesignators")]
	internal static class ReverseDesignatorDatabase_Init_Patch {
		[HarmonyPostfix]
		public static void InjectReverseDesignators(ReverseDesignatorDatabase __instance) {
			AllowToolController.Instance.OnReverseDesignatorDatabaseInit(__instance);
		}
	}
}