using HarmonyLib;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Automatically unforbids any killed creatures during drafted hunting if the appropriate setting is enabled.   
	/// </summary>
	[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill), typeof(DamageInfo?), typeof(Hediff))]
	internal static class Pawn_Kill_Patch {
		[HarmonyPostfix]
		public static void UnforbidDraftedHuntBody(Pawn __instance, DamageInfo? dinfo) {
			var perpetrator = dinfo?.Instigator as Pawn;
			var worldSettings = AllowToolController.Instance.WorldSettings?.PartyHunt;
			if (perpetrator != null && worldSettings != null
				&& worldSettings.UnforbidDrops && worldSettings.PawnIsPartyHunting(perpetrator)) {
				__instance.Corpse?.SetForbidden(false, false);
			}
		}
	}
}