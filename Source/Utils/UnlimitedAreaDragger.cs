using System;
using HugsLib;
using Verse;

namespace AllowTool {
	/// <summary>
	/// A complement to the vanilla <see cref="DesignationDragger"/> that gives finer control over the designation process.
	/// Also, allows designation of areas larger than the default 50x50.
	/// </summary>
	public class UnlimitedAreaDragger {
		/// <summary>
		/// Player started to drag a new selection
		/// </summary>
		public event Action<CellRect> SelectionStart;
		/// <summary>
		/// The size of the selection changed.
		/// </summary>
		public event Action<CellRect> SelectionChanged;
		/// <summary>
		/// Player finished dragging the selection
		/// </summary>
		public event Action<CellRect> SelectionComplete;
		/// <summary>
		/// Called each frame while selection is in progress
		/// </summary>
		public event Action<CellRect> SelectionUpdate;

		private Designator owningDesignator;
		private bool listening;
		private bool updateScheduled;

		public bool SelectionInProgress { get; private set; }
		public CellRect SelectedArea { get; private set; }
		public IntVec3 SelectionStartCell { get; private set; } = IntVec3.Invalid;

		public void BeginListening(Designator parentDesignator) {
			owningDesignator = parentDesignator;
			listening = true;
			RegisterForNextUpdate();
		}

		public void StopListening() {
			listening = false;
		}

		private void OnSelectionStarted() {
			SelectionInProgress = true;
			SelectionStartCell = ClampPositionToMapRect(Find.CurrentMap, UI.MouseCell());
			SelectionStart?.Invoke(CellRect.SingleCell(SelectionStartCell));
		}

		private void OnSelectedAreaChanged() {
			SelectionChanged?.Invoke(SelectedArea);
		}

		private void OnSelectionEnded() {
			SelectionInProgress = false;
			SelectionComplete?.Invoke(SelectedArea);
			SelectionStartCell = IntVec3.Invalid;
			SelectedArea = CellRect.Empty;
		}

		private void Update() {
			updateScheduled = false;
			var map = Find.CurrentMap;
			if (listening && map == null || Find.MapUI.designatorManager.SelectedDesignator != owningDesignator) {
				StopListening();
			}
			if (!listening) return;
			RegisterForNextUpdate();
			var dragger = Find.MapUI.designatorManager.Dragger;
			if (!SelectionInProgress && dragger.Dragging) {
				OnSelectionStarted();
			} else if (SelectionInProgress && !dragger.Dragging) {
				OnSelectionEnded();
			}
			if (SelectionInProgress) {
				var mouseCell = UI.MouseCell();
				var currentCell = ClampPositionToMapRect(map, mouseCell);
				var currentRect = CellRect.FromLimits(SelectionStartCell, currentCell);
				if (currentRect != SelectedArea) {
					SelectedArea = currentRect;
					OnSelectedAreaChanged();
				}
				SelectionUpdate?.Invoke(SelectedArea);
			}
		}

		private IntVec3 ClampPositionToMapRect (Map map, IntVec3 pos) {
			return CellRect.WholeMap(map).ClosestCellTo(pos);
		}

		private void RegisterForNextUpdate() {
			if(updateScheduled) return;
			HugsLibController.Instance.DoLater.DoNextUpdate(Update);
			updateScheduled = true;
		}
	}
}
