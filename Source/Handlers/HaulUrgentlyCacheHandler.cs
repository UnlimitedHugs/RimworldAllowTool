using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
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
		private readonly NoStorageSpaceTracker noStorageTracker;

		public HaulUrgentlyCacheHandler() {
			noStorageTracker = new NoStorageSpaceTracker(this);
		}

		public IReadOnlyList<Thing> GetDesignatedThingsForMap(Map map, float currentTime) {
			RecacheIfNeeded(map, currentTime);
			return cacheEntries[map].DesignatedThings;
		}

		public IReadOnlyList<Thing> GetDesignatedAndHaulableThingsForMap(Map map, float currentTime) {
			RecacheIfNeeded(map, currentTime);
			return cacheEntries[map].DesignatedHaulableThings;
		}

		public List<GlobalTargetInfo> GetDesignatedThingsWithoutStorageSpace() {
			return noStorageTracker.GetDesignatedThingsWithoutStorage();
		}

		public void ClearCacheForMap(Map map) {
			cacheEntries.Remove(map);
		}

		public void ClearCacheForAllMaps() {
			cacheEntries.Clear();
		}

		public void ProcessCacheEntries(int currentFrame, float currentTime) {
			if (AllowToolController.Instance.Handles.StorageSpaceAlertSetting.Value) {
				var map = Find.CurrentMap;
				if (map != null) {
					noStorageTracker.ProcessDesignations(map, currentFrame, currentTime);
				} else {
					noStorageTracker.ClearCache();
				}
			}
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
				if (des.def == AllowToolDefOf.HaulUrgentlyDesignation && des.target.Thing != null) {
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

		private class NoStorageSpaceTracker {
			private const int RecacheUpdateInterval = 50;

			private readonly HaulUrgentlyCacheHandler cacheHandler;
			private readonly List<CacheEntry> targetCache = new List<CacheEntry>();
			private readonly List<GlobalTargetInfo> outputList = new List<GlobalTargetInfo>();
			private readonly Comparison<GlobalTargetInfo> consistentTargetOrderComparer =
				(t1, t2) => t1.Thing.thingIDNumber.CompareTo(t2.Thing.thingIDNumber);
			private int cachedForMapId = -1;
			private HashSet<Thing> reservedThingsCache;
			private int reservedThingsCacheExpirationUpdate = int.MinValue;

			public NoStorageSpaceTracker(HaulUrgentlyCacheHandler cacheHandler) {
				this.cacheHandler = cacheHandler;
			}

			public void ProcessDesignations(Map map, int currentUpdate, float currentTime) {
				PruneExpiredEntries();
				VerifyCachedMap();
				var designatedThings = cacheHandler.GetDesignatedThingsForMap(map, currentTime);
				if (designatedThings.Count == 0) return;
				UpdateCachedReservations();
				ProcessDesignatedThings();

				void PruneExpiredEntries() {
					for (var i = targetCache.Count - 1; i >= 0; i--) {
						if (currentUpdate > targetCache[i].ExpirationUpdate) {
							targetCache.RemoveAt(i);
						} else {
							// entries are sorted in reverse expiration order, so it's safe to skip the rest of the list
							break;
						}
					}
				}

				void VerifyCachedMap() {
					if (cachedForMapId != map.uniqueID) {
						cachedForMapId = map.uniqueID;
						ClearCache();
					}
				}

				void UpdateCachedReservations() {
					if (currentUpdate > reservedThingsCacheExpirationUpdate + RecacheUpdateInterval) {
						reservedThingsCacheExpirationUpdate = currentUpdate + RecacheUpdateInterval;
						reservedThingsCache = GetReservedThingsOnMap(map);
					}
				}

				void ProcessDesignatedThings() {
					// this setup distributes the workload over multiple fixed updates and caches the results
					// thingIDNumber is used for discrimination, so the distribution is biased
					// but it tends to only process one or two thing per update, which meets our goal
					var currentUpdateProcessingOffset = currentUpdate % RecacheUpdateInterval;
					for (var i = 0; i < designatedThings.Count; i++) {
						var thing = designatedThings[i];
						var thingShouldBeProcessedThisUpdate =
							thing.thingIDNumber % RecacheUpdateInterval == currentUpdateProcessingOffset;
						if (thingShouldBeProcessedThisUpdate
							&& thing.Spawned
							&& !reservedThingsCache.Contains(thing)
							&& HasNoHaulDestination(thing)) {
							// prepending new elements ensures that expired entries are always at the end of the list 
							targetCache.Insert(0,
								new CacheEntry(currentUpdate + RecacheUpdateInterval - 1, new GlobalTargetInfo(thing)));
						}
					}
				}
			}

			public List<GlobalTargetInfo> GetDesignatedThingsWithoutStorage() {
				outputList.Clear();
				for (var i = 0; i < targetCache.Count; i++) {
					outputList.Add(targetCache[i].Target);
				}
				// the list of targets rotates as cache is refreshed. Sort guarantees a consistent order for the tooltip
				outputList.Sort(consistentTargetOrderComparer);
				return outputList;
			}

			public void ClearCache() {
				targetCache.Clear();
			}

			private HashSet<Thing> GetReservedThingsOnMap(Map map) {
				// reservations on a storage tile can cause false positives
				// we have no easy way to detect the reserved tile is for this exact item
				// so, we ignore items that have any reservation on them, as we assume they are about to be hauled
				return new HashSet<Thing>(map.reservationManager.AllReservedThings());
			}

			private bool HasNoHaulDestination(Thing t) {
				return !StoreUtility.TryFindBestBetterStorageFor(t, null, t.Map,
					StoreUtility.CurrentStoragePriorityOf(t), Faction.OfPlayer, out _, out _);
			}

			private readonly struct CacheEntry {
				public int ExpirationUpdate { get; }
				public GlobalTargetInfo Target { get; }

				public CacheEntry(int expirationUpdate, GlobalTargetInfo target) {
					ExpirationUpdate = expirationUpdate;
					Target = target;
				}
			}
		}
	}
}