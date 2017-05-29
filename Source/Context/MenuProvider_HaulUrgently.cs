using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_HaulUrgently : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_urgent"; }
		}

		public override string SettingId {
			get { return "providerHaulUrgently"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_HaulUrgently); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.HaulableEver; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, "Designator_context_urgent_visible", HaulVisibleAction);
			yield return MakeMenuOption(designator, "Designator_context_urgent_all", HaulEverythingAction);
		}
		
		// hotkey activation
		public override void ContextMenuAction(Designator designator, Map map) {
			HaulVisibleAction(designator, map);
		}

		// skip rock chunks in designation, select only visible on screen
		private void HaulVisibleAction(Designator designator, Map map) {
			var visibleRect = GetVisibleMapRect();
			DesignateWithPredicate(designator,map, thing => visibleRect.Contains(thing.Position));
		}

		private void HaulEverythingAction(Designator designator, Map map) {
			DesignateWithPredicate(designator, map);
		}

		private void DesignateWithPredicate(Designator designator, Map map, Func<Thing, bool> shouldDesignateThing = null) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (ValidForDesignation(thing) &&
					designator.CanDesignateThing(thing).Accepted && 
					!thing.def.designateHaulable && 
					(shouldDesignateThing == null || shouldDesignateThing(thing))) {
					
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}

		// code swiped from ThingSelectionUtility
		private static CellRect GetVisibleMapRect() {
			var screenRect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
			var screenLoc1 = new Vector2(screenRect.x, UI.screenHeight - screenRect.y);
			var screenLoc2 = new Vector2(screenRect.x + screenRect.width, UI.screenHeight - (screenRect.y + screenRect.height));
			var corner1 = UI.UIToMapPosition(screenLoc1);
			var corner2 = UI.UIToMapPosition(screenLoc2);
			return new CellRect {
				minX = Mathf.FloorToInt(corner1.x),
				minZ = Mathf.FloorToInt(corner2.z),
				maxX = Mathf.FloorToInt(corner2.x),
				maxZ = Mathf.FloorToInt(corner1.z)
			};
		}
	}
}