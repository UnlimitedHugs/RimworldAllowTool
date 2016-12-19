using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool {
	/**
	 * This is an alternative DesignationDragger that allows unlimited area selection of selectable things.
	 * Must be activated on demand by designator that require this functionality.
	 */
	public class UnlimitedDesignationDragger {
		public delegate bool ThingIsReleveantFilter(Thing item);

		private readonly HashSet<IntVec3> affectedCells = new HashSet<IntVec3>(); 
		private ThingIsReleveantFilter filterCallback;
		private Material dragHighlightMat;
		private Designator invokingDesignator;
		private bool listening;
		private bool draggerActive;
		private IntVec3 mouseDownPosition;

		public void BeginListening(ThingIsReleveantFilter callback, Texture2D dragHighlightTex) {
			listening = true;
			filterCallback = callback;
			dragHighlightMat = MaterialPool.MatFrom(dragHighlightTex, ShaderDatabase.MetaOverlay, Color.white);
			invokingDesignator = Find.MapUI.designatorManager.SelectedDesignator;
		}

		public IEnumerable<IntVec3> GetAffectedCells() {
			return affectedCells;
		}

		public void Update() {
			if (!listening) return;
			if (Current.ProgramState != ProgramState.Playing || Find.VisibleMap == null) {
				listening = false;
				return;
			}
			if (Find.MapUI.designatorManager.SelectedDesignator != invokingDesignator) {
				listening = false;
				return;
			}
			var dragger = Find.MapUI.designatorManager.Dragger;
			if (!draggerActive && dragger.Dragging) {
				mouseDownPosition = UI.MouseCell();
				draggerActive = true;
			} else if (draggerActive && !dragger.Dragging) {
				draggerActive = false;
			}
			if (draggerActive) {
				var mouseCell = UI.MouseCell();
				UpdateAffectedCellsInRect(mouseDownPosition, mouseCell);
				DrawOverlayOnCells(affectedCells);
			}
		}

		private void UpdateAffectedCellsInRect(IntVec3 pos1, IntVec3 pos2) {
			affectedCells.Clear();
			var map = Find.VisibleMap;
			if (map == null) return;
			// estabilish bounds
			int minX, maxX, minZ, maxZ;
			if (pos1.x <= pos2.x) {
				minX = pos1.x;
				maxX = pos2.x;
			} else {
				minX = pos2.x;
				maxX = pos1.x;
			}
			if (pos1.z <= pos2.z) {
				minZ = pos1.z;
				maxZ = pos2.z;
			} else {
				minZ = pos2.z;
				maxZ = pos1.z;
			}

			// check all items against bounds
			var allTheThings = map.listerThings.AllThings;
			for (var i = 0; i < allTheThings.Count; i++) {
				var thing = allTheThings[i];
				var thingPos = thing.Position;
				if (thing.def.selectable && thingPos.x >= minX && thingPos.x <= maxX && thingPos.z >= minZ && thingPos.z <= maxZ && filterCallback(thing)) {
					affectedCells.Add(thingPos);
				}
			}
		}

		private void DrawOverlayOnCells(IEnumerable<IntVec3> cells) {
			foreach (var cell in cells) {
				Graphics.DrawMesh(MeshPool.plane10, cell.ToVector3Shifted() + 10f * Vector3.up, Quaternion.identity, dragHighlightMat, 0);	
			}
		}
	}
}
