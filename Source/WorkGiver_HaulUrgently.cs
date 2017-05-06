using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	// Generates hauling jobs for things designated for urgent hauling
	public class WorkGiver_HaulUrgently : WorkGiver_Scanner {
		
		public override Job JobOnThing(Pawn pawn, Thing t) {
			return HaulAIUtility.HaulToStorageJob(pawn, t);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var designations = pawn.Map.designationManager.allDesignations;
			// for all designated cells
			for (int i = 0; i < designations.Count; i++) {
				var des = designations[i];
				if (des.def == AllowToolDefOf.HaulUgentlyDesignation) {
					// get a list of things
					var thingList = pawn.Map.thingGrid.ThingsListAt(des.target.Cell);
					for (int index = 0; index < thingList.Count; index++) {
						var thing = thingList[index];
						// that are yet to be hauled to storage
						if (thing.def != null && 
							(thing.def.alwaysHaulable || thing.def.EverHaulable) && 
							!thing.IsInValidBestStorage() &&
							HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, thing)) {
							yield return thing;
						}
					}
				}
			}
			
		}
	}
}