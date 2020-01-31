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

		public override bool Active {
			get {
				if(!AllowToolController.Instance.Handles.StorageSpaceAlertSetting.Value) return false;
				RecacheIfNeeded();
				return cachedHaulablesWithoutDestination.Count > 0;
			}
		}

		public override AlertPriority Priority {
			get { return AlertPriority.High; }
		}

		public Alert_NoUrgentStorage() {
			defaultLabel = "Alert_noStorage_label".Translate();
		}

		public override string GetExplanation() {
			var culprits = cachedHaulablesWithoutDestination.Select(t => t.Thing?.LabelShort)
				.Take(MaxListedCulpritsInExplanation).ToList();
			if (culprits.Count < cachedHaulablesWithoutDestination.Count) culprits.Add("...");
			return "Alert_noStorage_desc".Translate(culprits.ListElements());
		}

		public override AlertReport GetReport() {
			return Active ? AlertReport.CulpritsAre(cachedHaulablesWithoutDestination) : AlertReport.Inactive;
		}

		protected override Color BGColor {
			get { return new Color(1f, 0.9215686f, 0.01568628f, .35f); }
		}

		private void RecacheIfNeeded() {
			var map = Find.CurrentMap;
			if (map != null && (cachedForMapIndex != map.Index || lastRecacheFrame + RecacheFrameInterval < Time.frameCount)) {
				cachedForMapIndex = map.Index;
				lastRecacheFrame = Time.frameCount;
				cachedHaulablesWithoutDestination.Clear();
				var allDesignations = map.designationManager.allDesignations;
				for (var i = 0; i < allDesignations.Count; i++) {
					var des = allDesignations[i];
					if (des.def == AllowToolDefOf.HaulUrgentlyDesignation) {
						var thing = des.target.Thing;
						if (thing != null && thing.Spawned && HasNoHaulDestination(thing)) {
							cachedHaulablesWithoutDestination.Add(new GlobalTargetInfo(thing));
						}
					}
				}
			}
		}

		private bool HasNoHaulDestination(Thing t) {
			return !StoreUtility.TryFindBestBetterStorageFor(t, null, t.Map, StoreUtility.CurrentStoragePriorityOf(t), 
				Faction.OfPlayer, out IntVec3 _, out IHaulDestination _);
		}
	}
}