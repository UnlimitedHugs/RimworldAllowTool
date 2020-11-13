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

		public IReadOnlyList<Thing> GetHaulablesForMap(Map map, int currentTick) {
			RecacheIfNeeded(map, currentTick);
			return cacheEntries[map].Things;
		}

		public void ClearCacheForMap(Map map) {
			cacheEntries.Remove(map);
		}

		public void ClearCacheForAllMaps() {
			cacheEntries.Clear();
		}

		private void RecacheIfNeeded(Map map, int currentTick) {
			if (!cacheEntries.TryGetValue(map, out var entry) || !entry.IsValid(currentTick)) {
				var things = entry.Things ?? new List<Thing>();
				GetHaulUrgentlyDesignatedHaulables(map, things);
				cacheEntries[map] = new ThingsCacheEntry(currentTick, things);
			}
		}

		private void GetHaulUrgentlyDesignatedHaulables(Map map, ICollection<Thing> targetList) {
			targetList.Clear();
			workThingsSet.AddRange(map.listerHaulables.ThingsPotentiallyNeedingHauling());
			var mapDesignations = map.designationManager.allDesignations;
			for (var i = 0; i < mapDesignations.Count; i++) {
				var des = mapDesignations[i];
				if(des.def == AllowToolDefOf.HaulUrgentlyDesignation && workThingsSet.Contains(des.target.Thing)) {
					targetList.Add(des.target.Thing);
				}
			}
			workThingsSet.Clear();
		}

		private readonly struct ThingsCacheEntry {
			public List<Thing> Things { get; }
			private const int ForcedExpireTimeTicks = 1 * GenTicks.TicksPerRealSecond;
			
			private readonly int createdTick;

			public ThingsCacheEntry(int currentTick, List<Thing> things) {
				Things = things;
				createdTick = currentTick;
			}

			public bool IsValid(int currentTick) {
				return currentTick > 0 && currentTick <= createdTick + ForcedExpireTimeTicks;
			}
		}
	}
}