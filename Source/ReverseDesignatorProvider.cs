using AllowTool.Context;
using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Injects custom reverse designators- the ones that show up when appropriate items are selected
	/// </summary>
	public static class ReverseDesignatorProvider {
		public static void InjectCustomReverseDesignators(ReverseDesignatorDatabase database) {
			var designatorsList = database.AllDesignators;
			// inject a chop designator to compensate for the normalized Designator_PlantsCut
			designatorsList.Add(new Designator_PlantsHarvestWood());
			// inject our custom designators
			foreach (var def in DefDatabase<ReverseDesignatorDef>.AllDefs) {
				if (AllowToolController.Instance.IsReverseDesignatorEnabledInSettings(def)) {
					var des = AllowToolController.Instance.InstantiateDesignator(def.designatorClass, def.designatorDef);
					if (Current.Game.Rules.DesignatorAllowed(des)) {
						designatorsList.Add(des);
					}
				}
			}
			DesignatorContextMenuController.PrepareReverseDesignatorContextMenus();
		}
	}
}