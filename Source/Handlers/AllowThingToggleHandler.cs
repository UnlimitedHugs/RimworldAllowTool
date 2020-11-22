using System.Collections.Generic;
using AllowTool.Context;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Adds reverse designator-like functionality to the allow/forbid toggle on selected things. 
	/// </summary>
	internal static class AllowThingToggleHandler {
		private static Designator allowDesignatorStandIn = new Designator_Allow();
		private static Designator forbidDesignatorStandIn = new Designator_Forbid();

		public static void EnhanceStockAllowToggle(Command_Toggle toggle) {
			var standInDesignator = toggle.isActive() ? allowDesignatorStandIn : forbidDesignatorStandIn;
			DesignatorContextMenuController.RegisterReverseDesignatorPair(standInDesignator, toggle);
			AddIconReplacementSupport(toggle, standInDesignator);
		}

		public static IEnumerable<Designator> GetImpliedReverseDesignators() {
			yield return allowDesignatorStandIn;
			yield return forbidDesignatorStandIn;
		}

		public static void ReinitializeDesignators() {
			// recreating the designators allows them to respond to mod settings changes
			allowDesignatorStandIn = new Designator_Allow();
			forbidDesignatorStandIn = new Designator_Forbid();
		}

		private static void AddIconReplacementSupport(Command_Toggle toggle, Designator standInDesignator) {
			if (AllowToolController.Instance.Handles.ReplaceIconsSetting.Value) {
				toggle.icon = standInDesignator.icon;
			}
		}
	}
}