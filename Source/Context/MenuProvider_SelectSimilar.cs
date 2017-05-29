using System;
using System.Collections.Generic;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Selects similar things to those already selected across the entire map
	/// </summary>
	public class MenuProvider_SelectSimilar : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_similar"; }
		}

		public override string SettingId {
			get { return "providerSelectSimilar"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_SelectSimilar); }
		}

		public override void ContextMenuAction(Designator designator, Map map) {
			var des = (Designator_SelectSimilar) designator;
			des = des.GetNonReverseVersion();
			if (Find.Selector.NumSelected == 0) {
				Messages.Message("Designator_context_similar_fail".Translate(), MessageSound.RejectInput);
				return;
			}
			
			des.ReindexSelectionConstraints();
			// find things to select
			List<Thing> thingsToSelect = new List<Thing>();
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

			if (limitWasHit) {
				Messages.Message("Designator_context_similar_part".Translate(hitCount, thingsToSelect.Count), MessageSound.Benefit);
			} else {
				Messages.Message("Designator_context_similar_succ".Translate(hitCount), MessageSound.Benefit);
			}
		}
	}
}