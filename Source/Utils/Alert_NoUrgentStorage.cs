using System.Linq;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Displayed when items designated with Haul Urgently have no storage to haul them to
	/// </summary>
	public class Alert_NoUrgentStorage : Alert {
		private const int MaxListedCulpritsInExplanation = 5;

		public override AlertPriority Priority {
			get { return AlertPriority.High; }
		}

		public Alert_NoUrgentStorage() {
			defaultLabel = "Alert_noStorage_label".Translate();
		}

		public override TaggedString GetExplanation() {
			var alertCulpritTargets =
				AllowToolController.Instance.HaulUrgentlyCache.GetDesignatedThingsWithoutStorageSpace(); 
			var culpritThings = alertCulpritTargets.Select(t => t.Thing?.LabelShort)
				.Take(MaxListedCulpritsInExplanation).ToList();
			if (alertCulpritTargets.Count > MaxListedCulpritsInExplanation) culpritThings.Add("...");
			return "Alert_noStorage_desc".Translate(culpritThings.ListElements());
		}

		public override AlertReport GetReport() {
			if (AllowToolController.Instance.Handles.StorageSpaceAlertSetting.Value) {
				var alertCulprits =
					AllowToolController.Instance.HaulUrgentlyCache.GetDesignatedThingsWithoutStorageSpace(); 
				if (alertCulprits.Count > 0) {
					return AlertReport.CulpritsAre(alertCulprits);
				}
			}
			return AlertReport.Inactive;
		}

		protected override Color BGColor {
			get { return new Color(1f, 0.9215686f, 0.01568628f, .35f); }
		}
	}
}