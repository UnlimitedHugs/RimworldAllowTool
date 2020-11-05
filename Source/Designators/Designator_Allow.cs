using RimWorld;

namespace AllowTool {
	/// <summary>
	/// A replacement for the stock Allow designator.
	/// Confers all the benefits provided by <see cref="Designator_SelectableThings"/>.
	/// </summary>
	public class Designator_Allow : Designator_Replacement {
		public Designator_Allow() {
			SetReplacedDesignator(new Designator_Unforbid());
			UseDesignatorDef(AllowToolDefOf.AllowDesignator);
		}
	}
}