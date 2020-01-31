using System;
using System.Linq;
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
			var des = AllowToolUtility.EnumerateResolvedDirectDesignators().OfType<Designator_SelectSimilar>().FirstOrDefault();
			if (des == null) {
				throw new Exception("The Select Similar designator must exist somewhere in the Architect categories for this to work. " +
									"It can be hidden in the Allow Tool mod options if desired.");
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