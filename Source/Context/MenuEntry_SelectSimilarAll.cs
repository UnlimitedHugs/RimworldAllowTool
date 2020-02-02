using System;
using System.Collections.Generic;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_SelectSimilarAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_similar";
		protected override string SettingHandleSuffix => "selectSimilarAll";

		public override ActivationResult Activate(Designator designator, Map map) {
			return SelectSimilarWithFilter(designator, map, BaseMessageKey, BaseMessageKey);
		}

		public static ActivationResult SelectSimilarWithFilter(Designator designator, Map map,
			string successMessageKey, string failureMessageKey, Predicate<Thing> filter = null) {
			var des = (Designator_SelectSimilar)designator;
			des = (Designator_SelectSimilar)des.PickUpReverseDesignator();

			if (Find.Selector.NumSelected == 0) {
				return ActivationResult.Failure(failureMessageKey);
			}

			des.ReindexSelectionConstraints();
			// find things to select
			var thingsToSelect = new List<Thing>();
			foreach (var thing in map.listerThings.AllThings) {
				if (thing != null && (filter == null || filter(thing)) && des.CanDesignateThing(thing).Accepted) {
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
				? ActivationResult.SuccessMessage((successMessageKey + "_part").Translate(hitCount, thingsToSelect.Count))
				: ActivationResult.Success(successMessageKey, hitCount);
		}
	}
}