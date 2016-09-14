using RimWorld;
using Verse;

namespace AllowTool {
	public class Designator_Allow : Designator_SelectableThings {
		public Designator_Allow(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			var comp = item is ThingWithComps ? (item as ThingWithComps).GetComp<CompForbiddable>() : null;
			return comp != null && comp.Forbidden;
		}

		override protected int ProcessCell(IntVec3 c) {
			var hitCount = 0;
			var cellThings = Find.ThingGrid.ThingsListAtFast(c);
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (thing.def.selectable && thing.IsForbidden(Faction.OfPlayer)) {
					thing.SetForbidden(false);
					hitCount++;
				}
			}
			return hitCount;
		}
	}
}