using System.Collections.Generic;
using HarmonyLib;
using Verse;

// Appends the "party hunt" toggle to the pawn buttons.
namespace AllowTool.Patches {
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	internal static class Pawn_GetGizmos_Patch {
		[HarmonyPostfix]
		public static void InsertPartyHuntGizmo(Pawn __instance, ref IEnumerable<Gizmo> __result) {
			var toggle = PartyHuntHandler.TryGetGizmo(__instance);
			if (toggle != null) {
				__result = AppendGizmo(__result, toggle);
			}
		}

		private static IEnumerable<Gizmo> AppendGizmo(IEnumerable<Gizmo> originalSequence, Gizmo addition) {
			foreach (var gizmo in originalSequence) {
				yield return gizmo;
			}
			yield return addition;
		}

	}
}