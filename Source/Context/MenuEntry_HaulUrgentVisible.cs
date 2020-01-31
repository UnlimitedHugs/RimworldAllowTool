using System;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HaulUrgentVisible : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_urgent_visible";
		protected override string BaseMessageKey => "Designator_context_urgent";
		protected override string SettingHandleSuffix => "haulUrgentVisible";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.HaulableEver;
		public override Type HandledDesignatorType => typeof(Designator_HaulUrgently);

		public override ActivationResult Activate(Designator designator, Map map) {
			var visibleRect = GetVisibleMapRect();
			var hitCount = DesignateAllThings(designator, map, 
				t => MenuEntry_HaulUrgentAll.CanAutoDesignateThingForUrgentHauling(t) && visibleRect.Contains(t.Position));
			return hitCount > 0 ?
				ActivationResult.Success(BaseTextKey, hitCount) : 
				ActivationResult.Failure(BaseMessageKey);
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