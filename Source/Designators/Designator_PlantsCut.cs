namespace AllowTool {
	/// <summary>
	/// A replacement for the stock "Cut plants" designator.
	/// Confers all the benefits provided by <see cref="Designator_SelectableThings"/>.
	/// </summary>
	public class Designator_PlantsCut : Designator_Replacement {
		public Designator_PlantsCut() {
			SetReplacedDesignator(new RimWorld.Designator_PlantsCut());
			UseDesignatorDef(AllowToolDefOf.ATPlantsCutDesignator);
		}
	}
}