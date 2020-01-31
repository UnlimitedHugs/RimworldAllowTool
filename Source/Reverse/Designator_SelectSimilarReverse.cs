using Verse;

namespace AllowTool {
	/// <summary>
	/// A stub for use as a reverse designator.
	/// We can't use the regular one, because the selection constraints mess with the visibility of the reverse designator.
	/// Instead of designating, picks up the actual SelectSimilar designator.
	/// </summary>
	public class Designator_SelectSimilarReverse : Designator_SelectSimilar {
		public override bool ReversePickingAllowed {
			get { return false; }
		}

		public Designator_SelectSimilar GetNonReverseVersion() {
			return new Designator_SelectSimilar();
		}

		public override AcceptanceReport CanDesignateThing(Thing thing) {
			return thing.def != null &&
			       thing.def.selectable &&
			       thing.def.label != null &&
			       thing.Map != null &&
			       !thing.Map.fogGrid.IsFogged(thing.Position);
		}

		protected override void FinalizeDesignationSucceeded() {
			var selectSimilarNonReverse = GetNonReverseVersion();
			Find.DesignatorManager.Select(selectSimilarNonReverse);
			base.FinalizeDesignationSucceeded();
		}
	}
}