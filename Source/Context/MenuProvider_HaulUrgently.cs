using System;
using RimWorld;
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

		// skip rock chunks in designation, select only visible on screen
		public override void ContextMenuAction(Designator designator, Map map) {
			var visibleRect = GetVisibleMapRect();
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (visibleRect.Contains(thing.Position) && designator.CanDesignateThing(thing).Accepted && !thing.def.designateHaulable) {
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