using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Displayed when items designated with Haul Urgently have no storage to haul them to
	/// </summary>
	public class Alert_NoUrgentStorage : Alert {
		private const int RecacheFrameInterval = 60;
		private const int MaxListedCulpritsInExplanation = 5;

		private readonly List<GlobalTargetInfo> cachedHaulablesWithoutDestination = new List<GlobalTargetInfo>();
		private int cachedForMapIndex = -1;
		private int lastRecacheFrame;

		public override AlertPriority Priority {
			get { return AlertPriority.High; }
		}

		public Alert_NoUrgentStorage() {
			defaultLabel = "Alert_noStorage_label".Translate();
		}

		public override TaggedString GetExplanation() {
			var culprits = cachedHaulablesWithoutDestination.Select(t => t.Thing?.LabelShort)
				.Take(MaxListedCulpritsInExplanation).ToList();
			if (culprits.Count < cachedHaulablesWithoutDestination.Count) culprits.Add("...");
			return "Alert_noStorage_desc".Translate(culprits.ListElements());
		}

		public override AlertReport GetReport() {
			if (AllowToolController.Instance.Handles.StorageSpaceAlertSetting.Value) {
				RecacheIfNeeded();
				if (cachedHaulablesWithoutDestination.Count > 0) {
					return AlertReport.CulpritsAre(cachedHaulablesWithoutDestination);
				}
			}
			return AlertReport.Inactive;
		}

		protected override Color BGColor {
			get { return new Color(1f, 0.9215686f, 0.01568628f, .35f); }
		}

		private void RecacheIfNeeded() {
			var map = Find.CurrentMap;
			if (map != null && (cachedForMapIndex != map.Index || lastRecacheFrame + RecacheFrameInterval < Time.frameCount)) {
				var reservedThings = GetReservedThingsOnMap(map);
				cachedForMapIndex = map.Index;
				lastRecacheFrame = Time.frameCount;
				cachedHaulablesWithoutDestination.Clear();
				var allDesignations = map.designationManager.allDesignations;
				for (var i = 0; i < allDesignations.Count; i++) {
					var des = allDesignations[i];
					if (des.def == AllowToolDefOf.HaulUrgentlyDesignation) {
						var thing = des.target.Thing;
						if (thing != null
							&& thing.Spawned
							&& !reservedThings.Contains(thing)
							&& HasNoHaulDestination(thing)) {
							cachedHaulablesWithoutDestination.Add(new GlobalTargetInfo(thing));
						}
					}
				}
			}
		}

		private HashSet<Thing> GetReservedThingsOnMap(Map map) {
			// reservations on a storage tile can cause false positives
			// we have no easy way to detect the reserved tile is for this exact item
			// so, we ignore items that have any reservation on them, as we assume they are about to be hauled
			return new HashSet<Thing>(map.reservationManager.AllReservedThings());
		}

		private bool HasNoHaulDestination(Thing t) {
			return !StoreUtility.TryFindBestBetterStorageFor(t, null, t.Map, StoreUtility.CurrentStoragePriorityOf(t), 
				Faction.OfPlayer, out IntVec3 _, out IHaulDestination _);
		}
	}
}