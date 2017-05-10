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

	}
}