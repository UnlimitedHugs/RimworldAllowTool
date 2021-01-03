using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Unforbids all forbidden things in the designated area
	/// </summary>
	public class Designator_Allow : Designator_Replacement {
		public Designator_Allow() {
			replacedDesignator = new Designator_Unforbid();
			UseDesignatorDef(AllowToolDefOf.AllowDesignator);
		}

		public override AcceptanceReport CanDesignateThing(Thing thing) {
			if (thing.Position.Fogged(thing.Map)) return false;
			var comp = (thing as ThingWithComps)?.GetComp<CompForbiddable>();
			return comp != null && comp.Forbidden;
		}

		public override void DesignateThing(Thing t) {
			t.SetForbidden(false, false);
		}
	}
}