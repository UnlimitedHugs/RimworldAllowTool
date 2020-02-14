using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Forbids all forbiddable things in the designated area
	/// </summary>
	public class Designator_Forbid : Designator_Replacement {
		public Designator_Forbid() {
			replacedDesignator = new RimWorld.Designator_Forbid();
			UseDesignatorDef(AllowToolDefOf.ForbidDesignator);
		}

		public override AcceptanceReport CanDesignateThing(Thing thing) {
			if (thing.Position.Fogged(thing.Map)) return false;
			var comp = (thing as ThingWithComps)?.GetComp<CompForbiddable>();
			return comp != null && !comp.Forbidden;
		}

		public override void DesignateThing(Thing t) {
			t.SetForbidden(true, false);
		}
	}
}