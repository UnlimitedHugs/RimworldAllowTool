using RimWorld;
using Verse;

namespace AllowTool {
	internal static class SpecialTreeMassDesignationFix {
		public static AcceptanceReport RejectSpecialTreeMassDesignation(Thing designated, AcceptanceReport originalReport) {
			return originalReport.Accepted && IsSpecialTree(designated) && MassDesignationInProgress()
				? AcceptanceReport.WasRejected
				: originalReport;
		}

		public static bool IsSpecialTree(Thing t) {
			return t.def == ThingDefOf.Plant_TreeAnima
				|| t.def == ThingDefOf.Plant_TreeGauranlen;
		}

		private static bool MassDesignationInProgress() {
			return Find.DesignatorManager.Dragger is var dr && dr.Dragging && dr.DragCells.Count > 1;
		}
	}
}