using RimWorld;
using Verse;

namespace AllowTool {
	public class Designator_Forbid : Designator_SelectableThings {
		public Designator_Forbid(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			var comp = item is ThingWithComps ? (item as ThingWithComps).GetComp<CompForbiddable>() : null;
			return comp != null && !comp.Forbidden;
		}

		protected override int ProcessCell(IntVec3 c) {
			var hitCount = 0;
			var cellThings = Find.ThingGrid.ThingsListAtFast(c);
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				var comp = thing is ThingWithComps ? (thing as ThingWithComps).GetComp<CompForbiddable>() : null;
				if (comp!=null && thing.def.selectable && !comp.Forbidden) {
					comp.Forbidden = true;
					hitCount++;
				}
			}
			return hitCount;
		}
	}
}