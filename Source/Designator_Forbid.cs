using RimWorld;
using Verse;

namespace AllowTool {
	// Forbids all forbiddable things in the designated area
	public class Designator_Forbid : Designator_SelectableThings {
		public Designator_Forbid(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			if (item.Position.Fogged(item.Map)) return false;
			var comp = item is ThingWithComps ? (item as ThingWithComps).GetComp<CompForbiddable>() : null;
			return comp != null && !comp.Forbidden;
		}

		protected override int ProcessCell(IntVec3 c) {
			return AllowToolUtility.ToggleForbiddenInCell(c, Find.VisibleMap, true);
		}
	}
}