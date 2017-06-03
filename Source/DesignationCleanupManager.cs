using System.Collections.Generic;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Periodically cleans up designations that no longer have valid targets.
	/// </summary>
	public static class DesignationCleanupManager {
		private const int TickInterval = 60;

		public static void Tick(int currentTick) {
			if (currentTick%TickInterval != 0) return;
			if (Current.Game == null || Current.Game.Maps == null) return;
			CleanupFinishOffDesignations();
		}

		private static void CleanupFinishOffDesignations() {
			var maps = Find.Maps;
			List<Designation> cleanupList = null;
			for (int i = 0; i < maps.Count; i++) {
				var map = maps[i];
				if(map.designationManager == null) continue;
				var mapDesignations = map.designationManager.allDesignations;
				for (int j = 0; j < mapDesignations.Count; j++) {
					var des = mapDesignations[j];
					if (des.def == AllowToolDefOf.FinishOffDesignation && !Designator_FinishOff.IsValidDesignationTarget(des.target.Thing)) {
						if (cleanupList == null) {
							cleanupList = new List<Designation>();
						}
						cleanupList.Add(des);
					}
				}
			}
			if (cleanupList != null) {
				foreach (var designation in cleanupList) {
					designation.designationManager.RemoveDesignation(designation);
				}
			}
		}
	}
}