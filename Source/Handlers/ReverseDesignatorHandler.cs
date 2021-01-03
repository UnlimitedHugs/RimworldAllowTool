using System;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Injects custom reverse designators- the ones that show up when appropriate items are selected
	/// </summary>
	public static class ReverseDesignatorHandler {
		internal static void InjectReverseDesignators(ReverseDesignatorDatabase database) {
			var designatorsList = database.AllDesignators;
			// inject our custom designators
			foreach (var def in DefDatabase<ReverseDesignatorDef>.AllDefs) {
				try {
					if (AllowToolController.Instance.Handles.IsReverseDesignatorEnabled(def)) {
						var des = InstantiateThingDesignator(def);
						if (Current.Game.Rules.DesignatorAllowed(des)) {
							designatorsList.Add(des);
						}
					}
				} catch (Exception e) {
					throw new Exception("Failed to create reverse designator", e);
				}
			}
			// ensure newly created designators have context menus
			AllowToolController.Instance.ScheduleDesignatorDependencyRefresh();
		}

		private static Designator InstantiateThingDesignator(ReverseDesignatorDef reverseDef) {
			var designatorType = reverseDef.designatorClass ?? reverseDef.designatorDef.designatorClass;
			try {
				return (Designator)Activator.CreateInstance(designatorType);
			} catch (Exception e) {
				throw new Exception($"Failed to instantiate designator {designatorType.FullName} (def {reverseDef.defName})", e);
			}
		}
	}
}