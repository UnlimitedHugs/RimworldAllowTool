using System;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Cut : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_cut"; }
		}

		public override string SettingId {
			get { return "providerCut"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_PlantsCut); }
		}

		protected override ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}

		public override void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesignatorRequestGroup)) {
				var plant = thing as Plant;
				if (plant == null || !ValidForDesignation(plant) || !plant.Blighted || plant.HasDesignation(DesignationDefOf.CutPlant)) continue;
				hitCount++;
				plant.ToggleDesignation(DesignationDefOf.CutPlant, true);
			}
			ReportActionResult(hitCount, EntryTextKey);
		}
	}
}