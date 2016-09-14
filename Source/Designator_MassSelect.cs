using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Designator_MassSelect : Designator_SelectableThings {
		private bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		private bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		}

		public Designator_MassSelect(ThingDesignatorDef def) : base(def) {
		}

		protected override bool ThingIsRelevant(Thing item) {
			return true;
		}

		public override void DesignateSingleCell(IntVec3 loc) {
			if (!ShiftIsHeld) Find.Selector.ClearSelection();
			base.DesignateSingleCell(loc);
			CloseArchitectMenu();
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if (!ShiftIsHeld) Find.Selector.ClearSelection();
			base.DesignateMultiCell(cells);
			CloseArchitectMenu();
		}

		protected override int ProcessCell(IntVec3 cell) {
			var cellThings = Find.ThingGrid.ThingsListAtFast(cell);
			var selectedObjects = Find.Selector.SelectedObjects;
			var hits = 0;
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (!thing.def.selectable) continue;
				if (selectedObjects.Contains(thing)) {
					if (ControlIsHeld) {
						selectedObjects.Remove(thing);
						hits++;
					}
				} else {
					selectedObjects.Add(thing);
					SelectionDrawer.Notify_Selected(thing);
					hits++;
				}
			}
			return hits;
		}

		private void CloseArchitectMenu() {
			if (Find.Selector.NumSelected == 0) return;
			if(Find.MainTabsRoot.OpenTab != MainTabDefOf.Architect) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}
	}
}