using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool {
	// a complement to DesignationDragger to allow for huge area selection of items
	[StaticConstructorOnStartup]
	public class ItemDesignationDragger {
		private static readonly Material dragHighlightCellMat = MaterialPool.MatFrom("UI/Overlays/DragHighlightCell", ShaderDatabase.MetaOverlay);

		public delegate bool ItemIsReleveantFilter(Thing item);

		private readonly ItemIsReleveantFilter filterCallback;
		private IntVec3 mouseDownPosition;
		private List<IntVec3> affectedCells;

		public ItemDesignationDragger(ItemIsReleveantFilter filterCallback) {
			this.filterCallback = filterCallback;
		}

		public bool Listening { get; private set; }

		public void BeginListening() {
			mouseDownPosition = Gen.MouseCell();
			Listening = true;
		}

		public void FinishListening() {
			Listening = false;
		}

		public List<IntVec3> GetAffectedCells() {
			return affectedCells;
		}


		public void DraggerUpdate() {
			if(!Listening) return;
			var mouseCell = Gen.MouseCell();
			affectedCells = GetCellsWithItemsInRect(mouseDownPosition, mouseCell);
			DrawOverlayOnCells(affectedCells);
		}


		private List<IntVec3> GetCellsWithItemsInRect(IntVec3 pos1, IntVec3 pos2) {
			var resultCells = new List<IntVec3>();

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
			var allTheThings = Find.ListerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
			for (int i = 0; i < allTheThings.Count; i++) {
				var thing = allTheThings[i];
				var thingPos = thing.Position;
				if (thingPos.x >= minX && thingPos.x <= maxX && thingPos.z >= minZ && thingPos.z <= maxZ && filterCallback(thing)) {
					resultCells.Add(thingPos);
				}
			}

			return resultCells;
		}

		private void DrawOverlayOnCells(IEnumerable<IntVec3> cells) {
			foreach (var pos in cells) {
				Graphics.DrawMesh(MeshPool.plane10, pos.ToVector3Shifted() + 10f * Vector3.up, Quaternion.identity, dragHighlightCellMat, 0);	
			}
		}
	}
}
