using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Designator_MassSelect : Designator_SelectableThings {
		private const string ConstraintListSeparator = ", ";
		private const int MaxNumListedConstraints = 5;

		private readonly Dictionary<int, SelectionDefConstraint> selectionConstraints =  new Dictionary<int, SelectionDefConstraint>();
		private bool constraintsNeedReindexing;
		private string cachedConstraintReadout;
		private bool controlIsHeld;

		public Designator_MassSelect(ThingDesignatorDef def) : base(def) {
		}

		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			constraintsNeedReindexing = true;
		}

		protected override bool ThingIsRelevant(Thing item) {
			return !Find.FogGrid.IsFogged(item.Position) && (!controlIsHeld || ThingMatchesSelectionConstraints(item));
		}

		public override void DesignateSingleCell(IntVec3 loc) {
			if (!AllowToolUtility.ShiftIsHeld) Find.Selector.ClearSelection();
			base.DesignateSingleCell(loc);
			CloseArchitectMenu();
			constraintsNeedReindexing = true;
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if (!AllowToolUtility.ShiftIsHeld) Find.Selector.ClearSelection();
			base.DesignateMultiCell(cells);
			CloseArchitectMenu();
			constraintsNeedReindexing = true;
		}

		public override void SelectedOnGUI() {
			controlIsHeld = AllowToolUtility.ControlIsHeld;
			if (!controlIsHeld) return;
			if (constraintsNeedReindexing) UpdateSelectionConstraints();
			var label = "MassSelect_nowSelecting".Translate(cachedConstraintReadout);
			DrawMouseAttachedLabel(label);
		}

		// select selectables in a sigle cell
		protected override int ProcessCell(IntVec3 cell) {
			if (Find.FogGrid.IsFogged(cell)) return 0;
			var cellThings = Find.ThingGrid.ThingsListAtFast(cell);
			var selectedObjects = Find.Selector.SelectedObjects;
			var hits = 0;
			for (var i = 0; i < cellThings.Count; i++) {
				var thing = cellThings[i];
				if (!thing.def.selectable) continue;
				if (selectedObjects.Contains(thing)) continue;
				if (controlIsHeld && !ThingMatchesSelectionConstraints(thing)) continue;
				selectedObjects.Add(thing);
				SelectionDrawer.Notify_Selected(thing);
				hits++;
			}
			return hits;
		}

		private void CloseArchitectMenu() {
			if (Find.Selector.NumSelected == 0) return;
			if(Find.MainTabsRoot.OpenTab != MainTabDefOf.Architect) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}

		private void DrawMouseAttachedLabel(string text) {
			const float CursorOffset = 12f;
			const float AttachedIconHeight = 32f;
			const float LabelWidth = 200f;
			var mousePosition = Event.current.mousePosition;
			if (text != string.Empty) {
				var rect = new Rect(mousePosition.x + CursorOffset, mousePosition.y + CursorOffset + AttachedIconHeight, LabelWidth, 9999f);
				Text.Font = GameFont.Small;
				Widgets.Label(rect, text);
			}
		}

		private void UpdateSelectionConstraints() {
			constraintsNeedReindexing = false;
			selectionConstraints.Clear();
			// get defs of selected objects, count duplicates
			foreach (var selectedObject in Find.Selector.SelectedObjects) {
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
					builder.Append("MassSelect_numMoreTypes".Translate(constraintList.Count - i));
					break;
				}
			}
			cachedConstraintReadout = builder.ToString();
			if (cachedConstraintReadout.Length == 0) cachedConstraintReadout = "MassSelect_anything".Translate(); // nothing was selected
		}

		private bool ThingMatchesSelectionConstraints(Thing thing) {
			return selectionConstraints.Count == 0 || selectionConstraints.ContainsKey(GetConstraintHashForThing(thing));
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