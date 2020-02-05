using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Base class for all designators that use a dragger to operate on things, rather than cells.
	/// </summary>
	public abstract class Designator_SelectableThings : Designator_UnlimitedDragger {
		private const float HighlightedCellsRecacheInterval = .5f;

		private static readonly List<Vector3> cachedHighlightQuadPositions = new List<Vector3>();
		private Material dragHighlightMat;
		private float nextHighlightRecacheTime;

		protected override void OnDefAssigned() {
			Def.GetDragHighlightexture(tex => {
				dragHighlightMat = MaterialPool.MatFrom(tex, ShaderDatabase.MetaOverlay, Color.white);
			});
		}

		public override void Selected() {
			base.Selected();
			Dragger.SelectionStart += DraggerOnSelectionStart;
			Dragger.SelectionChanged += DraggerOnSelectionChanged;
			Dragger.SelectionComplete += DraggerOnSelectionComplete;
			Dragger.SelectionUpdate += DraggerOnSelectionUpdate;
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

		private void DraggerOnSelectionStart(CellRect rect) {
			RecacheHighlightCellPositions(rect);
		}

		private void DraggerOnSelectionChanged(CellRect rect) {
			RecacheHighlightCellPositions(rect);
		}

		private void DraggerOnSelectionComplete(CellRect rect) {
			RecacheHighlightCellPositions(rect);
		}

		private void DraggerOnSelectionUpdate(CellRect rect) {
			if (Time.time >= nextHighlightRecacheTime) {
				RecacheHighlightCellPositions(rect);
			}
			DrawCachedCellHighlights();
		}

		private void RecacheHighlightCellPositions(CellRect rect) {
			nextHighlightRecacheTime = Time.time + HighlightedCellsRecacheInterval;
			cachedHighlightQuadPositions.Clear();
			if (Dragger.SelectionInProgress) {
				var allTheThings = Map.listerThings.AllThings;
				var drawOnTopOffset = 10f * Vector3.up;
				for (var i = 0; i < allTheThings.Count; i++) {
					var thing = allTheThings[i];
					if (thing.def.selectable && rect.Contains(thing.Position) && CanDesignateThing(thing).Accepted) {
						cachedHighlightQuadPositions.Add(thing.Position.ToVector3Shifted() + drawOnTopOffset);
					}
				}
			}
		}

		private void DrawCachedCellHighlights() {
			for (var i = 0; i < cachedHighlightQuadPositions.Count; i++) {
				Graphics.DrawMesh(MeshPool.plane10, cachedHighlightQuadPositions[i], Quaternion.identity, dragHighlightMat, 0);	
			}
		}
	}
}
