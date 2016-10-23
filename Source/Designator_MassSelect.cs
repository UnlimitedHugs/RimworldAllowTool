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
		
		private enum OperationMode {
			Normal,
			Constrained,
			AllOfDef
		}

		private readonly Dictionary<int, SelectionDefConstraint> selectionConstraints =  new Dictionary<int, SelectionDefConstraint>();
		private bool constraintsNeedReindexing;
		private string cachedConstraintReadout;
		private bool addToSelection;
		private OperationMode mode;

		// When in AllOfDef mode, dragging is disabled
		public override int DraggableDimensions {
			get { return mode == OperationMode.AllOfDef ? 0 : 2; }
		}
		
		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			if (mode == OperationMode.AllOfDef) {
				return TryGetItemOrPawnUnderCursor() != null;
			} else {
				return base.CanDesignateCell(c);
			}
		}

		public Designator_MassSelect(ThingDesignatorDef def) : base(def) {
		}

		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			constraintsNeedReindexing = true;
		}

		protected override bool ThingIsRelevant(Thing item) {
			return !Find.FogGrid.IsFogged(item.Position) && (mode != OperationMode.Constrained || ThingMatchesSelectionConstraints(item));
		}

		public override void DesignateSingleCell(IntVec3 loc) {
			if (!addToSelection) Find.Selector.ClearSelection();
			if (mode == OperationMode.AllOfDef) {
				var target = TryGetItemOrPawnUnderCursor();
				if(target == null) return;
				var numHits = SelectAllOfDef(target.def);
				if (numHits > 0) Messages.Message("Mass_Select_success".Translate(numHits, target.def.label.CapitalizeFirst()), MessageSound.Silent); 
			} else {
				base.DesignateSingleCell(loc);
			}
			TryCloseArchitectMenu();
			constraintsNeedReindexing = true;
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if (!addToSelection) Find.Selector.ClearSelection();
			base.DesignateMultiCell(cells);
			TryCloseArchitectMenu();
			constraintsNeedReindexing = true;
		}

		public override void SelectedOnGUI() {
			mode = OperationMode.Normal;
			if(AllowToolUtility.ControlIsHeld) mode = OperationMode.Constrained;
			if(AllowToolUtility.AltIsHeld) mode = OperationMode.AllOfDef;
			addToSelection = AllowToolUtility.ShiftIsHeld;
			if (mode == OperationMode.Constrained) {
				if (constraintsNeedReindexing) UpdateSelectionConstraints();
				var label = "MassSelect_nowSelecting".Translate(cachedConstraintReadout);
				DrawMouseAttachedLabel(label);
			} else if (mode == OperationMode.AllOfDef) {
				if (Event.current.type == EventType.Repaint) {
					var target = TryGetItemOrPawnUnderCursor();
					string label = target == null ? "MassSelect_needTarget".Translate() : "MassSelect_targetHover".Translate(target.def.label.CapitalizeFirst());
					DrawMouseAttachedLabel(label);
				}
			}
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
				if (mode == OperationMode.Constrained && !ThingMatchesSelectionConstraints(thing)) continue;
				selectedObjects.Add(thing);
				SelectionDrawer.Notify_Selected(thing);
				hits++;
			}
			return hits;
		}

		// selects all things with the same def and stuff def
		private int SelectAllOfDef(ThingDef targetDef) {
			if(targetDef == null) return 0;
			var things = Find.ListerThings.AllThings;
			var selectedObjects = Find.Selector.SelectedObjects;
			var hits = 0;
			for (int i = 0; i < things.Count; i++) {
				var thing = things[i];
				if (thing.def != targetDef || Find.FogGrid.IsFogged(thing.Position) || selectedObjects.Contains(thing)) continue;
				selectedObjects.Add(thing);
				SelectionDrawer.Notify_Selected(thing);
				hits++;
			}
			return hits;
		}

		private void TryCloseArchitectMenu() {
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

		private Thing TryGetItemOrPawnUnderCursor() {
			var things = Find.ThingGrid.ThingsAt(Gen.MouseCell());
			foreach (var thing in things) {
				if (thing.def != null && thing.def.selectable && thing.def.label!=null && (thing.def.category == ThingCategory.Item || thing.def.category == ThingCategory.Pawn)) return thing;
			}
			return null;
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