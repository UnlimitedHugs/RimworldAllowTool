using AllowTool.Context;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Injects custom reverse designators- the ones that show up when appropriate items are selected
	/// </summary>
	public static class ReverseDesignatorProvider {
		public static void OnReverseDesignatorInit(ReverseDesignatorDatabase database) {
			var designatorsList = database.AllDesignators;
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