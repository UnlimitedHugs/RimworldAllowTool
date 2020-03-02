using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Adds the "Finish off" forced job to the context menu of drafted pawns
	/// </summary>
	[HarmonyPatch(typeof(ThingWithComps), "GetFloatMenuOptions")]
	//public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
	internal static class Thing_GetFloatMenuOptions_Patch {
		[HarmonyPostfix]
		public static void FinishOffWhenDrafted(ref IEnumerable<FloatMenuOption> __result, Thing __instance, Pawn selPawn) {
			var floatOption = WorkGiver_FinishOff.InjectThingFloatOptionIfNeeded(__instance, selPawn);
			if (floatOption != null) {
				var opts = __result.ToList();
				opts.Add(floatOption);
				__result = opts;
			}
		}
	}
}