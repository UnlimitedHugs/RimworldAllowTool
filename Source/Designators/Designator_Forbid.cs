namespace AllowTool {
	/// <summary>
	/// A replacement for the stock Forbid designator.
	/// Confers all the benefits provided by <see cref="Designator_SelectableThings"/>.
	/// </summary>
	public class Designator_Forbid : Designator_Replacement {
		public Designator_Forbid() {
			SetReplacedDesignator(new RimWorld.Designator_Forbid());
			UseDesignatorDef(AllowToolDefOf.ForbidDesignator);
		}
	}
}