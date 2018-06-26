using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// Generates rearm jobs for traps designated for urgent rearming.
	/// Code mostly taken from WorkGiver_RearmTraps, just changing the designation to RearmUrgentlyDesignation
	/// </summary>
	public class WorkGiver_RearmUrgently : WorkGiver_RearmTraps {
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			foreach (var des in pawn.Map.designationManager.SpawnedDesignationsOfDef(AllowToolDefOf.RearmUrgentlyDesignation)) {
				yield return des.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			bool result;
			if (!t.HasDesignation(AllowToolDefOf.RearmUrgentlyDesignation)) {
				result = false;
			} else {
				LocalTargetInfo target = t;
				if (!pawn.CanReserve(target, 1, -1, null, forced)) {
					result = false;
				} else {
					var thingList = t.Position.GetThingList(t.Map);
					for (int i = 0; i < thingList.Count; i++) {
						if (thingList[i] != t && thingList[i].def.category == ThingCategory.Item) {
							IntVec3 intVec;
							if (thingList[i].IsForbidden(pawn) || thingList[i].IsInValidStorage() || !HaulAIUtility.CanHaulAside(pawn, thingList[i], out intVec)) {
								return false;
							}
						}
					}
					result = true;
				}
			}
			return result;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var thingList = t.Position.GetThingList(t.Map);
			for (int i = 0; i < thingList.Count; i++) {
				if (thingList[i] != t && thingList[i].def.category == ThingCategory.Item) {
					var job = HaulAIUtility.HaulAsideJobFor(pawn, thingList[i]);
					if (job != null) return job;
				}
			}
			return new Job(AllowToolDefOf.RearmTrapUrgently, t);
		}
	}
}