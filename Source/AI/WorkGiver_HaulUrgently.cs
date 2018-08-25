using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	// Generates hauling jobs for things designated for urgent hauling
	public class WorkGiver_HaulUrgently : WorkGiver_Scanner {

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			// give a vanilla haul job- it works just fine for our needs
			return HaulAIUtility.HaulToStorageJob(pawn, t);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var designations = pawn.Map.designationManager.allDesignations;
			// look over all designations
			for (int i = 0; i < designations.Count; i++) {
				var des = designations[i];
				// find our haul designation
				if (des.def != AllowToolDefOf.HaulUrgentlyDesignation) continue;
				var thing = des.target.Thing;
				// make sure the designated thing is a valid candidate for hauling
				if (thing?.def != null && (thing.def.alwaysHaulable || thing.def.EverHaulable) 
					&& !thing.IsInValidBestStorage() && HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing, false)) {
					yield return thing;
				}
			}
		}
	}
}