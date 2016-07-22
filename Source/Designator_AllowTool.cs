using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Designator_AllowTool : Designator {

		private readonly string successMessage = "AllowTool_area_success".Translate();
		private readonly ItemDesignationDragger dragger = new ItemDesignationDragger(ItemIsSelectable);

		private static bool ItemIsSelectable(Thing item) {
			return item.IsForbidden(Faction.OfPlayer);
		}

		public override int DraggableDimensions {
			get {
				return 2;
			}
		}

		public override bool DragDrawMeasurements {
			get {
				return true;
			}
		}

		public Designator_AllowTool() {
			defaultLabel = "AllowTool_label".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/Widgets/CheckOn");
			defaultDesc = "AllowTool_desc".Translate();
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.DesignateDragStandard;
			soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
			soundSucceeded = SoundDefOf.TickHigh;
			hotKey = KeyBindingDefOf.Misc6;
		}

		// this is for the default dragger. we will sample our area with ItemDesignationDragger
		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			return false;
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			var hitCount = 0;
			foreach (var cell in dragger.GetAffectedCells()) {
				var hits = AllowCell(cell);
				hitCount += hits;
			}
			if (hitCount > 0) {
				if (successMessage.Length > 0) {
					Messages.Message(successMessage.Replace("%", hitCount.ToString()), MessageSound.Silent);
				}
				FinalizeDesignationSucceeded();
			} else {
				Messages.Message("AllowTool_area_failed".Translate(), MessageSound.RejectInput);
			}
		}

		public override void DesignateSingleCell(IntVec3 loc) {
			if (AllowCell(loc)>0) {
				FinalizeDesignationSucceeded();
			} else {
				FinalizeDesignationFailed();
			}
		}

		// tool selected
		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			ModInitializerComponent.ScheduleUpdateCallback(OnGUICallback);
		}

		private void OnGUICallback() {
			if(DesignatorManager.Dragger.Dragging && !dragger.Listening) {
				dragger.BeginListening();
			} else if (!DesignatorManager.Dragger.Dragging && dragger.Listening) {
				dragger.FinishListening();
			}

			dragger.DraggerUpdate();
			
			if(DesignatorManager.SelectedDesignator==this) {
				ModInitializerComponent.ScheduleUpdateCallback(OnGUICallback);
			}
		}

		private int AllowCell(IntVec3 c) {
			var hitCount = 0;
			foreach (Thing current in Find.ThingGrid.ThingsAt(c)) {
				if (current.IsForbidden(Faction.OfPlayer)) {
					current.SetForbidden(false);
					hitCount++;
				}
			}
			return hitCount;
		}
	}
}
