using System.Collections.Generic;
using RimWorld;
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
			var things = AllowToolController.Instance.HaulUrgentlyCache.GetHaulablesForMap(
				pawn.Map, Find.TickManager.TicksGame);
			for (int i = 0; i < things.Count; i++) {
				if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, things[i], false)) {
					yield return things[i];
				}
			}
		}
	}
}