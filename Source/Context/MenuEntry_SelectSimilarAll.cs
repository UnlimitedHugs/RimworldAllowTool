using System;
using System.Collections.Generic;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_SelectSimilarAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_similar";
		protected override string SettingHandleSuffix => "selectSimilarAll";
		public override Type HandledDesignatorType => typeof(Designator_SelectSimilar);

		public override ActivationResult Activate(Designator designator, Map map) {
			var des = (Designator_SelectSimilar)designator;
			if (des is Designator_SelectSimilarReverse reverse) {
				des = reverse.GetNonReverseVersion();
			}
			if (Find.Selector.NumSelected == 0) {
				return ActivationResult.Failure(BaseMessageKey);
			}

			des.ReindexSelectionConstraints();
			// find things to select
			var thingsToSelect = new List<Thing>();
			foreach (var thing in map.listerThings.AllThings) {
				if (des.CanDesignateThing(thing).Accepted) {
					thingsToSelect.Add(thing);
				}
			}

			// sort by distance to camera
			var cameraCenter = Current.CameraDriver.MapPosition;
			thingsToSelect.SortBy(t => t.Position.DistanceTo(cameraCenter));

			// do selection
			var hitCount = 0;
			var limitWasHit = false;
			foreach (var thing in thingsToSelect) {
				if (!des.SelectionLimitAllowsAdditionalThing()) {
					limitWasHit = true;
					break;
				}
				if (des.TrySelectThing(thing)) {
					hitCount++;
				}
			}

			return limitWasHit
				? ActivationResult.SuccessMessage("Designator_context_similar_part".Translate(hitCount, thingsToSelect.Count))
				: ActivationResult.Success(BaseMessageKey, hitCount);
		}
	}
}