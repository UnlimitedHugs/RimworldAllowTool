using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// A Chop designator that selects only fully grown trees
	/// </summary>
	public class Designator_ChopFullyGrown : Designator_PlantsHarvestWood {
		public override AcceptanceReport CanDesignateThing(Thing t) {
			var result = base.CanDesignateThing(t);
			if (result.Accepted) {
				var plant = t as Plant;
				if (plant != null && plant.LifeStage == PlantLifeStage.Mature) {
					result = true;
				} else {
					result = "MessageMustDesignateHarvestableWood".Translate();
				}
			}
			return result;
		}

		public override void DrawMouseAttachments() {
			base.DrawMouseAttachments();
			AllowToolUtility.DrawMouseAttachedLabel("ChopFullyGrown_cursorTip".Translate());
		}
	}
}