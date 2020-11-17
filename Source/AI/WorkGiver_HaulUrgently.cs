using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AllowTool {
	// Generates hauling jobs for things designated for urgent hauling
	public class WorkGiver_HaulUrgently : WorkGiver_Scanner {
		public delegate Job TryGetJobOnThing(Pawn pawn, Thing t, bool forced);
		
		// give a vanilla haul job- it works just fine for our needs
		public static TryGetJobOnThing JobOnThingDelegate = 
			(pawn, t, forced) => HaulAIUtility.HaulToStorageJob(pawn, t);

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			return JobOnThingDelegate(pawn, t, forced);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var things = GetHaulablesForPawn(pawn);
			for (int i = 0; i < things.Count; i++) {
				if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, things[i], false)) {
					yield return things[i];
				}
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false) {
			return GetHaulablesForPawn(pawn).Count == 0;
		}

		private static IReadOnlyList<Thing> GetHaulablesForPawn(Pawn pawn) {
			return AllowToolController.Instance.HaulUrgentlyCache.GetDesignatedAndHaulableThingsForMap(
				pawn.Map, Time.unscaledTime);
		}
	}
}