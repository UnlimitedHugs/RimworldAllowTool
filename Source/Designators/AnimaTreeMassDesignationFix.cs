using RimWorld;
using Verse;

namespace AllowTool {
	internal static class AnimaTreeMassDesignationFix {
		public static AcceptanceReport RejectAnimaTreeMassDesignation(Thing designated, AcceptanceReport originalReport) {
			return originalReport.Accepted && IsAnimaTree(designated) && MassDesignationInProgress()
				? AcceptanceReport.WasRejected
				: originalReport;
		}

		public static bool IsAnimaTree(Thing t) {
			return t.def == ThingDefOf.Plant_TreeAnima;
		}

		private static bool MassDesignationInProgress() {
			return Find.DesignatorManager.Dragger is var dr && dr.Dragging && dr.DragCells.Count > 1;
		}
	}
}