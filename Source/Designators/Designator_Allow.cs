using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Unforbids all forbidden things in the designated area
	/// </summary>
	public class Designator_Allow : Designator_SelectableThings {
		
		public Designator_Allow(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			if (item.Position.Fogged(item.Map)) return false;
			var comp = item is ThingWithComps ? (item as ThingWithComps).GetComp<CompForbiddable>() : null;
			return comp != null && comp.Forbidden;
		}

		override protected int ProcessCell(IntVec3 c) {
			return AllowToolUtility.ToggleForbiddenInCell(c, Find.VisibleMap, false);
		}
	}
}