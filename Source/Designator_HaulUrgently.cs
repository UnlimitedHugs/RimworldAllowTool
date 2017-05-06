using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool {
	// Designates things for urgent hauling.
	// For A16 this designates cells instead of things because it avoids the need to detour anything.
	// In A17 with the use of harmony we can hook into the PlaceHauledThingInCell (where the Haul designation is removed) to remove our own designation.
	public class Designator_HaulUrgently : Designator_SelectableThings {
		public static int CountDesignateableThingsInCell(IntVec3 cell, Map map) {
			var hitCount = 0;
			var cellThings = map.thingGrid.ThingsListAt(cell);
			for (var i = 0; i < cellThings.Count; i++) {
				if (ThingCanBeDesignated(cellThings[i])) {
					hitCount++;
				}
			}
			return hitCount;
		}

		protected override void FinalizeDesignationSucceeded() {
			base.FinalizeDesignationSucceeded();
			if (HugsLibUtility.ShiftIsHeld) {

				foreach (var colonist in Find.VisibleMap.mapPawns.FreeColonists) {
						colonist.jobs.CheckForJobOverride();
				}
				
			}
		}

		private static bool ThingCanBeDesignated(Thing item) {
			if (item.def == null || item.Position.Fogged(item.Map)) return false;
			return (item.def.alwaysHaulable || item.def.EverHaulable) && !item.IsInValidStorage();
		}
		
		public Designator_HaulUrgently(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			return ThingCanBeDesignated(item);
		}
		
		protected override int ProcessCell(IntVec3 cell) {
			var map = Find.VisibleMap;
			var hitCount = CountDesignateableThingsInCell(cell, map);
			if (hitCount > 0) {
				// unforbid forbidden for convenience
				AllowToolUtility.ToggleForbiddenInCell(cell, map, false);
				TryAddHaulDesignatorToThingsInCell(cell, map);
				cell.ToggleDesignation(AllowToolDefOf.HaulUgentlyDesignation, true);
			}
			return hitCount;
		}

		// for things that require explicit hauling desingation, such as rock chunks
		private void TryAddHaulDesignatorToThingsInCell(IntVec3 cell, Map map) {
			var cellThings = map.thingGrid.ThingsListAtFast(cell);
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (thing.def != null && thing.def.designateHaulable) {
					thing.ToggleDesignation(DesignationDefOf.Haul, true);
				}
			}
		}
	}
}