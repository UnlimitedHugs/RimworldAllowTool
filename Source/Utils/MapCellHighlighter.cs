using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Draws a selection overlay over filtered map cells. 
	/// Caches highlighted cell positions for better performance.
	/// </summary>
	public class MapCellHighlighter {
		private readonly List<CachedHighlight> cachedHighlightQuadPositions = new List<CachedHighlight>();
		private readonly Func<IEnumerable<Request>> cellSelector;
		private readonly float recacheInterval;
		private readonly AltitudeLayer drawAltitude;

		private float nextHighlightRecacheTime;

		public MapCellHighlighter(Func<IEnumerable<Request>> cellSelector, 
			float recacheInterval = .5f, AltitudeLayer drawAltitude = AltitudeLayer.MetaOverlays) {
			this.cellSelector = cellSelector ?? throw new ArgumentNullException(nameof(cellSelector));
			this.recacheInterval = recacheInterval;
			this.drawAltitude = drawAltitude;
		}

		public void ClearCachedCells() {
			nextHighlightRecacheTime = 0f;
			cachedHighlightQuadPositions.Clear();
		}

		public void DrawCellHighlights() {
			if (Time.time >= nextHighlightRecacheTime) {
				RecacheCellPositions();
			}
			DrawCachedCellHighlights();
		}

		private void RecacheCellPositions() {
			nextHighlightRecacheTime = Time.time + recacheInterval;
			cachedHighlightQuadPositions.Clear();
			var altitudeOffset = drawAltitude.AltitudeFor();
			foreach (var request in cellSelector()) {
				cachedHighlightQuadPositions.Add(new CachedHighlight(
					new Vector3(request.Cell.x + .5f, altitudeOffset, request.Cell.z + .5f),
					request.Material)
				);
			}
		}

		private void DrawCachedCellHighlights() {
			for (var i = 0; i < cachedHighlightQuadPositions.Count; i++) {
				var values = cachedHighlightQuadPositions[i];
				Graphics.DrawMesh(MeshPool.plane10, values.DrawPosition, Quaternion.identity, values.Material, 0);	
			}
		}

		private Color Color32ToColor(Color32 c) {
			return new Color {
				r = c.r / (float)byte.MaxValue,
				g = c.g / (float)byte.MaxValue,
				b = c.b / (float)byte.MaxValue,
				a = c.a / (float)byte.MaxValue
			};
		}

		public class Request {
			public readonly IntVec3 Cell;
			public readonly Material Material;

			public Request(IntVec3 cell, Material material) {
				Cell = cell;
				Material = material;
			}
		}

		private class CachedHighlight {
			public readonly Vector3 DrawPosition;
			public readonly Material Material;

			public CachedHighlight(Vector3 drawPosition, Material material) {
				DrawPosition = drawPosition;
				Material = material;
			}
		}
	}
}