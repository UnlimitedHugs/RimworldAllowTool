using Harmony;
using RimWorld;

namespace AllowTool.Patches {
	/// <summary>
	/// Resets the "party hunting" whenever a pawn is undrafted
	/// </summary>
	[HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted", new []{typeof(bool)})]
	internal static class DraftController_Drafted_Patch {
		[HarmonyPostfix]
		public static void NotifyPawnUndrafted(Pawn_DraftController __instance, bool value) {
			if (!value) {
				PartyHuntController.OnPawnUndrafted(__instance.pawn);
			}
		}
	}
}