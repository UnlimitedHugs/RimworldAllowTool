using System;
using Verse;

namespace AllowTool {
	/// <summary>
	/// A stub for use as a reverse designator.
	/// We can't use the regular one, because the selection constraints mess with the visibility of the reverse designator.
	/// Instead of designating, picks up the actual SelectSimilar designator.
	/// </summary>
	public class Designator_SelectSimilarReverse : Designator_SelectSimilar {
		public Designator_SelectSimilarReverse(ThingDesignatorDef def) : base(def) {
		}

		public Designator_SelectSimilar GetNonReverseVersion() {
			var des = (Designator_SelectSimilar) AllowToolController.Instance.TryGetDesignator(AllowToolDefOf.SelectSimilarDesignator);
			if (des == null) {
				throw new Exception("Could not get Designator_SelectSimilar from AllowToolController");
			}
			return des;
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