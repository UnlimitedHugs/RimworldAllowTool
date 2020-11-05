namespace AllowTool {
	/// <summary>
	/// A replacement for the stock "Chop wood" designator.
	/// Confers all the benefits provided by <see cref="Designator_SelectableThings"/>.
	/// </summary>
	public class Designator_PlantsHarvestWood : Designator_Replacement {
		public Designator_PlantsHarvestWood() {
			SetReplacedDesignator(new RimWorld.Designator_PlantsHarvestWood());
			UseDesignatorDef(AllowToolDefOf.ATPlantsHarvestWoodDesignator);
		}
	}
}