using System.Collections.Generic;
using AllowTool.Context;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Adds reverse designator-like functionality to the allow/forbid toggle on selected things. 
	/// </summary>
	internal static class AllowThingToggleHandler {
		private static readonly Designator allowDesignatorStandIn = new Designator_Allow();
		private static readonly Designator forbidDesignatorStandIn = new Designator_Forbid();

		public static void EnhanceStockAllowToggle(Command_Toggle toggle) {
			var standInDesignator = toggle.isActive() ? allowDesignatorStandIn : forbidDesignatorStandIn;
			DesignatorContextMenuController.RegisterReverseDesignatorPair(standInDesignator, toggle);
		}
		
		public static IEnumerable<Designator> GetImpliedReverseDesignators() {
			yield return allowDesignatorStandIn;
			yield return forbidDesignatorStandIn;
		}
	}
}