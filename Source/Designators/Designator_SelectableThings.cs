using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Base class for all designators that use a dragger to operate on things, rather than cells.
	/// </summary>
	public abstract class Designator_SelectableThings : Designator_UnlimitedDragger {
		private Material highlightMaterial;

		protected Designator_SelectableThings() {
			var highlighter = new MapCellHighlighter(SelectHighlightedCells);
			Action<CellRect> clearHighlightedCells = r => highlighter.ClearCachedCells();
			Dragger.SelectionStart += clearHighlightedCells;
			Dragger.SelectionChanged += clearHighlightedCells;
			Dragger.SelectionComplete += clearHighlightedCells;
			Dragger.SelectionUpdate += r => highlighter.DrawCellHighlights();
		}

		protected override void OnDefAssigned() {
			Def.GetDragHighlightTexture(tex => 
				highlightMaterial = MaterialPool.MatFrom(tex, ShaderDatabase.MetaOverlay, Color.white)
			);
		}

		public override void DesignateSingleCell(IntVec3 cell) {
			numThingsDesignated = 0;
			var map = Find.CurrentMap;
			if (map == null) return;
			var things = map.thingGrid.ThingsListAt(cell);
			for (int i = 0; i < things.Count; i++) {
				var t = things[i];
				if (CanDesignateThing(t).Accepted) {
					DesignateThing(t);
					numThingsDesignated++;
				}
			}
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			var hitCount = 0;
			foreach (var cell in Dragger.SelectedArea) {
				DesignateSingleCell(cell);
				hitCount += numThingsDesignated;
			}
			if (hitCount > 0) {
				if (Def.messageSuccess != null) Messages.Message(Def.messageSuccess.Translate(hitCount.ToString()), MessageTypeDefOf.SilentInput);
				FinalizeDesignationSucceeded();
			} else {
				if (Def.messageFailure != null) Messages.Message(Def.messageFailure.Translate(), MessageTypeDefOf.RejectInput);
				FinalizeDesignationFailed();
			}
		}

		private IEnumerable<MapCellHighlighter.Request> SelectHighlightedCells() {
			var allTheThings = Map.listerThings.AllThings;
			for (var i = 0; i < allTheThings.Count; i++) {
				var thing = allTheThings[i];
				if (thing.def.selectable && Dragger.SelectedArea.Contains(thing.Position) && CanDesignateThing(thing).Accepted) {
					yield return new MapCellHighlighter.Request(thing.Position, highlightMaterial);
				}
			}
		}
	}
}
