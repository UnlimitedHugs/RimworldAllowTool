using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Designator_StripMine : Designator_UnlimitedDragger {
		private static readonly Material areaOutlineMaterial = 
			(Material)AllowToolController.Instance.Reflection.GenDrawLineMatMetaOverlay.GetValue(null);
		private readonly Action updateCallback;
		private readonly MapCellHighlighter highlighter;
		private CellRect currentSelection;
		private bool updateCallbackScheduled;

		public Designator_StripMine() {
			UseDesignatorDef(AllowToolDefOf.StripMineDesignator);
			highlighter = new MapCellHighlighter(EnumerateTargetCells);
			Dragger.SelectionStart += DraggerOnSelectionStart;
			Dragger.SelectionChanged += DraggerOnSelectionChanged;
			Dragger.SelectionComplete += DraggerOnSelectionComplete;
			updateCallback = OnUpdate;
		}

		protected override void OnDefAssigned() {
			Def.GetDragHighlightTexture(tex => highlighter.HighlightTexture = tex);
		}

		public override void Selected() {
			base.Selected();
			ScheduleUpdateCallback();
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			var currentCell = IntVec3.Invalid;
			try {
				var map = Find.CurrentMap;
				var mineDef = DesignationDefOf.Mine;
				var alreadyDesignatedCells = new HashSet<IntVec3>(map.designationManager
					.SpawnedDesignationsOfDef(mineDef)
					.Select(d => d.target.Cell)
				);
				foreach (var cell in EnumerateTargetCells()) {
					currentCell = cell;
					if (alreadyDesignatedCells.Contains(cell)) continue;
					cell.ToggleDesignation(DesignationDefOf.Mine, true);
				}
			} catch (Exception e) {
				AllowToolController.Logger.Error($"Error while placing Mine designation in {currentCell}: {e}");
			}
			currentSelection = CellRect.Empty;
		}

		private void OnUpdate() {
			updateCallbackScheduled = false;
			var map = Find.CurrentMap;
			if (map != null && Find.DesignatorManager.SelectedDesignator == this) {
				ScheduleUpdateCallback();
				DrawCellRectOutline(currentSelection);
				highlighter.DrawCellHighlights();
			} else {
				OnDesignatorDeselected();
			}
		}

		private void OnDesignatorDeselected() {
			currentSelection = CellRect.Empty;
			highlighter.ClearCachedCells();
		}

		private void ScheduleUpdateCallback() {
			if (updateCallbackScheduled) return;
			updateCallbackScheduled = true;
			HugsLibController.Instance.DoLater.DoNextUpdate(updateCallback);
		}

		private IEnumerable<IntVec3> EnumerateTargetCells() {
			var map = Find.CurrentMap;
			const int spacing = 4;
			bool CellIsOnGridLine(IntVec3 c) {
				return c.x % spacing == 0 || c.z % spacing == 0;
			}
			bool CellIsMineable(IntVec3 c) {
				if (c.Fogged(map)) return true;
				var m = c.GetFirstMineable(map);
				return m != null && m.def.mineable;
			}
			return currentSelection.Cells.Where(c => CellIsOnGridLine(c) && CellIsMineable(c));
		}

		private void DraggerOnSelectionStart(CellRect cellRect) {
			currentSelection = CellRect.Empty;
			highlighter.ClearCachedCells();
		}

		private void DraggerOnSelectionChanged(CellRect cellRect) {
			currentSelection = cellRect;
			highlighter.ClearCachedCells();
		}

		private void DraggerOnSelectionComplete(CellRect cellRect) {
			highlighter.ClearCachedCells();
		}

		private static void DrawCellRectOutline(CellRect rect) {
			if (rect.Area == 0) return;
			var altitude = AltitudeLayer.MoteLow.AltitudeFor();
			Vector3 bottomLeft = new Vector3(rect.minX, altitude, rect.minZ),
				bottomRight = new Vector3(rect.maxX + 1f, altitude, rect.minZ),
				topLeft = new Vector3(rect.minX, altitude, rect.maxZ + 1f),
				topRight = new Vector3(rect.maxX + 1f, altitude, rect.maxZ + 1f);
			GenDraw.DrawLineBetween(topLeft, topRight, areaOutlineMaterial);
			GenDraw.DrawLineBetween(bottomLeft, bottomRight, areaOutlineMaterial);
			GenDraw.DrawLineBetween(topLeft, bottomLeft, areaOutlineMaterial);
			GenDraw.DrawLineBetween(topRight, bottomRight, areaOutlineMaterial);
		}
	}
}