using System.Collections.Generic;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Injects custom reverse designators- the ones that show up when approptiate items are selected
	/// </summary>
	public static class ReverseDesignatorProvider {
		public static void OnReverseDesignatorInit(ReverseDesignatorDatabase database) {
			var designatorsList = (List<Designator>)AllowToolController.ReverseDesignatorDatabaseDesListField.GetValue(database);
			if (AllowToolController.Instance.IsDesignatorEnabledInSettings(AllowToolDefOf.HaulUrgentlyDesignator)) {
				var haulUrgently = new Designator_HaulUrgently(AllowToolDefOf.HaulUrgentlyDesignator);
				if (Current.Game.Rules.DesignatorAllowed(haulUrgently)) {
					designatorsList.Add(haulUrgently);
				}
			}
			if (AllowToolController.Instance.IsDesignatorEnabledInSettings(AllowToolDefOf.SelectSimilarDesignator)) {
				var selectSimilar = new Designator_SelectSimilar(AllowToolDefOf.SelectSimilarDesignator, true);
				if (Current.Game.Rules.DesignatorAllowed(selectSimilar)) {
					designatorsList.Add(selectSimilar);
				}
			}
		}
	}
}