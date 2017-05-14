using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	
	/// <summary>
	/// A tool to select things of the same Def as those already selected.
	/// Holding Shift allows picking additional things to select.
	/// Holding Alt will ignore all selection limits.
	/// </summary>
	public class Designator_SelectSimilar : Designator_SelectableThings {
		private const string ConstraintListSeparator = ", ";
		private const int MaxNumListedConstraints = 5;
		
		private readonly Dictionary<int, SelectionDefConstraint> selectionConstraints =  new Dictionary<int, SelectionDefConstraint>();
		private bool constraintsNeedReindexing;
		private string readableConstraintList;
		private UnlimitedDesignationDragger dragger;
		
		private Selector selectorRef;
		private Selector Selector { // cache selector to avoid lots of unnecessary routing calls
			get { return selectorRef ?? (selectorRef = Find.Selector); }
		}

		private int numDesignated;
		public override int GetNumDesigantedThings() {
			return numDesignated;
		}

		private bool AnySelectionContstraints {
			get { return selectionConstraints.Count > 0; }
		}

		public Designator_SelectSimilar(ThingDesignatorDef def) : base(def) {
		}

		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			ReindexSelectionConstraints();
			dragger = AllowToolController.Instance.Dragger;
		}

		public override AcceptanceReport CanDesignateThing(Thing thing) {
			return thing.def != null &&
				   thing.def.selectable &&
				   thing.def.label != null &&
				   !BlockedByFog(thing.Position, thing.Map) &&
				   (ThingMatchesSelectionConstraints(thing) || dragger.SelectingSingleCell) && // this allows us to select items that don't match the selection conststraints if we are not dragging, only clicking
				   SelectionLimitAllowsAdditionalThing();
		}

		public override void DesignateThing(Thing t) {
			TrySelectThing(t);
		}

		public override void DesignateSingleCell(IntVec3 cell) {
			var map = Find.VisibleMap;
			var cellThings = map.thingGrid.ThingsListAtFast(cell);
			numDesignated = 0;
			for (var i = 0; i < cellThings.Count; i++) {
				if (TrySelectThing(cellThings[i])) {
					numDesignated++;
				}
			}
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> vanillaCells) {
			var selectedCells = AllowToolController.Instance.Dragger.GetAffectedCells().ToList();
			// we have to check manually because DesignateSingleCell is not invoked for 2-dimensional designators
			if (dragger.SelectingSingleCell) {
				ProcessSincleCellClick(selectedCells.FirstOrDefault());
			} else {
				base.DesignateMultiCell(vanillaCells);
			}
			TryCloseArchitectMenu();
		}

		public override void SelectedOnGUI() {
			// update def filter and draw filter readout on cursor
			if (constraintsNeedReindexing) ReindexSelectionConstraints();
			string label;
			if (!SelectionLimitAllowsAdditionalThing()) {
				label = "SelectSimilar_cursor_limit".Translate();
			} else if (AnySelectionContstraints) {
				label = "SelectSimilar_cursor_nowSelecting".Translate(readableConstraintList);
			} else {
				label = "SelectSimilar_cursor_needConstraint".Translate();
			}
			DrawMouseAttachedLabel(label);
		}

		public bool SelectionLimitAllowsAdditionalThing() {
			return Selector.NumSelected < AllowToolController.Instance.SelectionLimitSetting.Value || dragger.SelectingSingleCell || HugsLibUtility.AltIsHeld;
		}

		// generate an index of defs to compare other things against, based on currently selected things
		public void ReindexSelectionConstraints() {
			try {
				constraintsNeedReindexing = false;
				selectionConstraints.Clear();
				readableConstraintList = "";
				if (Selector.NumSelected == 0) return;
				// get defs of selected objects, count duplicates
				foreach (var selectedObject in Selector.SelectedObjects) {
					var thing = selectedObject as Thing;
					if (thing == null || thing.def == null || !thing.def.selectable) continue;
					int constraintHash = GetConstraintHashForThing(thing);
					SelectionDefConstraint constraint;
					selectionConstraints.TryGetValue(constraintHash, out constraint);
					if (constraint == null) selectionConstraints[constraintHash] = constraint = new SelectionDefConstraint(thing.def, thing.Stuff);
					constraint.occurences++;
				}
				var constraintList = selectionConstraints.Values.ToList();
				var builder = new StringBuilder();
				constraintList.Sort((e1, e2) => -e1.occurences.CompareTo(e2.occurences)); // sort by number of occurences, descending
				// list constraints for the tooltip
				for (int i = 0; i < constraintList.Count; i++) {
					var isLastEntry = i >= constraintList.Count - 1;
					var constraint = constraintList[i];
					if (i < MaxNumListedConstraints - 1 || isLastEntry) {
						if (constraint.thingDef.label == null) continue;
						builder.Append(constraint.thingDef.label.CapitalizeFirst());
						if (constraint.stuffDef != null && constraint.stuffDef.label != null) {
							builder.AppendFormat(" ({0})", constraint.stuffDef.label.CapitalizeFirst());
						}
						if (!isLastEntry) builder.Append(ConstraintListSeparator);
					} else {
						builder.Append("SelectSimilar_numMoreTypes".Translate(constraintList.Count - i));
						break;
					}
				}
				readableConstraintList = builder.ToString();
			} catch (Exception e) {
				AllowToolController.Instance.Logger.ReportException(e);
			}
		}

		public bool TrySelectThing(Thing thing) {
			if (!CanDesignateThing(thing).Accepted || Selector.IsSelected(thing)) return false;
			Selector.SelectedObjects.Add(thing); // manually adding objects to the selection list gets around the stock selection limit
			SelectionDrawer.Notify_Selected(thing);
			if (!AnySelectionContstraints) {
				ReindexSelectionConstraints();
			} else {
				constraintsNeedReindexing = true;
			}
			return true;
		}

		private void ProcessSincleCellClick(IntVec3 cell) {
			if (!HugsLibUtility.ShiftIsHeld) {
				Selector.ClearSelection();
				ReindexSelectionConstraints();
			}
			if (cell.IsValid) {
				var things = Find.VisibleMap.thingGrid.ThingsAt(cell);
				foreach (var thing in things) {
					if (TrySelectThing(thing)) {
						break;
					}
				}
			}
		}

		// ignore fogged cells unless dev mode is on
		private bool BlockedByFog(IntVec3 cell, Map map) {
			return map.fogGrid.IsFogged(cell) && !DebugSettings.godMode;
		}

		// close architect menu if anything wa selected
		private void TryCloseArchitectMenu() {
			if (Selector.NumSelected == 0) return;
			if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.Architect) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}

		private void DrawMouseAttachedLabel(string text) {
			const float CursorOffset = 12f;
			const float AttachedIconHeight = 32f;
			const float LabelWidth = 200f;
			var mousePosition = Event.current.mousePosition;
			if (!text.NullOrEmpty()) {
				var rect = new Rect(mousePosition.x + CursorOffset, mousePosition.y + CursorOffset + AttachedIconHeight, LabelWidth, 9999f);
				Text.Font = GameFont.Small;
				Widgets.Label(rect, text);
			}
		}

		private bool ThingMatchesSelectionConstraints(Thing thing) {
			return !AnySelectionContstraints || selectionConstraints.ContainsKey(GetConstraintHashForThing(thing));
		}

		// Try to uniquely identify a thing/stuff combination
		private int GetConstraintHashForThing(Thing thing) {
			int hash = thing.def.shortHash;
			if (thing.Stuff != null)
				unchecked {
					hash += thing.Stuff.shortHash * 31;
				}
			return hash;
		}

		private class SelectionDefConstraint {
			public readonly Def thingDef;
			public readonly Def stuffDef;
			public int occurences = 1;

			public SelectionDefConstraint(Def thingDef, Def stuffDef) {
				this.thingDef = thingDef;
				this.stuffDef = stuffDef;
			}
		}
	}
}