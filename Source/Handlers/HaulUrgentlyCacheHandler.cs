using System.Collections.Generic;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Keeps track of things designated for urgent hauling on all maps.
	/// This optimizes performance, since we don't need to query for hauling
	/// and storage information each time we assign hauling jobs to pawns. 
	/// </summary>
	internal class HaulUrgentlyCacheHandler {
		private readonly Dictionary<Map, ThingsCacheEntry> cacheEntries = new Dictionary<Map, ThingsCacheEntry>();
		private readonly HashSet<Thing> workThingsSet = new HashSet<Thing>();

		public IReadOnlyList<Thing> GetDesignatedThingsForMap(Map map, float currentTime) {
			RecacheIfNeeded(map, currentTime);
			return cacheEntries[map].DesignatedThings;
		}

		public IReadOnlyList<Thing> GetDesignatedAndHaulableThingsForMap(Map map, float currentTime) {
			RecacheIfNeeded(map, currentTime);
			return cacheEntries[map].DesignatedHaulableThings;
		}

		public void ClearCacheForMap(Map map) {
			cacheEntries.Remove(map);
		}

		public void ClearCacheForAllMaps() {
			cacheEntries.Clear();
		}

		private void RecacheIfNeeded(Map map, float currentTime) {
			if (!cacheEntries.TryGetValue(map, out var entry) || !entry.IsValid(currentTime)) {
				var designated = entry.DesignatedThings ?? new List<Thing>();
				GetHaulUrgentlyDesignatedThings(map, designated);
				var designatedAndHaulable = entry.DesignatedHaulableThings ?? new List<Thing>();
				GetMapHaulables(map, designated, designatedAndHaulable);
				cacheEntries[map] = new ThingsCacheEntry(currentTime, designated, designatedAndHaulable);
			}
		}

		private void GetHaulUrgentlyDesignatedThings(Map map, ICollection<Thing> targetList) {
			targetList.Clear();
			var mapDesignations = map.designationManager.allDesignations;
			for (var i = 0; i < mapDesignations.Count; i++) {
				var des = mapDesignations[i];
				if (des.def == AllowToolDefOf.HaulUrgentlyDesignation) {
					targetList.Add(des.target.Thing);
				}
			}
		}
		
		private void GetMapHaulables(Map map, IReadOnlyList<Thing> intersectWith, ICollection<Thing> targetList) {
			targetList.Clear();
			for (var i = 0; i < intersectWith.Count; i++) {
				workThingsSet.Add(intersectWith[i]);
			}
			var haulables = map.listerHaulables.ThingsPotentiallyNeedingHauling();
			for (var i = 0; i < haulables.Count; i++) {
				if (workThingsSet.Contains(haulables[i])) targetList.Add(haulables[i]);
			}
			workThingsSet.Clear();
		}

		private readonly struct ThingsCacheEntry {
			private const float ExpireTime = 1f;
			
			public List<Thing> DesignatedThings { get; }
			public List<Thing> DesignatedHaulableThings { get; }

			private readonly float createdTime;

			public ThingsCacheEntry(float currentTime, List<Thing> designatedThings, List<Thing> 
			designatedHaulableThings) {
				createdTime = currentTime;
				DesignatedThings = designatedThings;
				DesignatedHaulableThings = designatedHaulableThings;
			}

			public bool IsValid(float currentTime) {
				return createdTime > 0f && currentTime < createdTime + ExpireTime;
			}
		}
	}
}