using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// A Harvest designator that selects only fully grown plants
	/// </summary>
	public class Designator_HarvestFullyGrown : Designator_PlantsHarvest {
		public override AcceptanceReport CanDesignateThing(Thing t) {
			var result =  base.CanDesignateThing(t);
			if (result.Accepted) {
				var plant = t as Plant;
				if (plant != null && plant.LifeStage == PlantLifeStage.Mature) {
					result = true;
				} else {
					result = "MessageMustDesignateHarvestable".Translate();
				}
			}
			return result;
		}
		
		public override void DrawMouseAttachments() {
			base.DrawMouseAttachments();
			AllowToolUtility.DrawMouseAttachedLabel("HarvestFullyGrown_cursorTip".Translate());
		}
	}
}