using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AllowTool {
	public static class AllowToolUtility {
		// unforbids forbidden things in a cell and returns the number of hits
		public static int ToggleForbiddenInCell(IntVec3 cell, Map map, bool makeForbidden) {
			if(map == null) throw new NullReferenceException("map is null");
			var hitCount = 0;
			List<Thing> cellThings;
			try {
				cellThings = map.thingGrid.ThingsListAtFast(cell);
			} catch (IndexOutOfRangeException e) {
				throw new IndexOutOfRangeException("Cell out of bounds: "+cell, e);
			}
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i] as ThingWithComps;
				if (thing != null && thing.def.selectable && thing.IsForbidden(Faction.OfPlayer) != makeForbidden) {
					thing.SetForbidden(makeForbidden);
					hitCount++;
				}
			}
			return hitCount;
		}


		// Checks if a cell has a designation of a given def
		public static bool HasDesignation(this IntVec3 pos, DesignationDef def, Map map = null) {
			if (map == null) {
				map = Find.VisibleMap;
			}
			if (map == null || map.designationManager == null) return false;
			return map.designationManager.DesignationAt(pos, def) != null;
		}

		// Adds or removes a designation of a given def on a cell. Fails silently if designation is already in the desired state.
		public static void ToggleDesignation(this IntVec3 pos, DesignationDef def, bool enable, Map map = null) {
			if (map == null) {
				map = Find.VisibleMap;
			}
			if (map == null || map.designationManager == null) throw new Exception("ToggleDesignation requires a map argument or VisibleMap must be set");
			var des = map.designationManager.DesignationAt(pos, def);
			if (enable && des == null) {
				map.designationManager.AddDesignation(new Designation(pos, def));
			} else if (!enable && des != null) {
				map.designationManager.RemoveDesignation(des);
			}
		}

	}
}