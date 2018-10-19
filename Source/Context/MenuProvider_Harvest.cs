using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Harvest : BaseDesignatorMenuProvider {
		private const string HarvestAllTextKey = "Designator_context_harvest";
		private const string HarvestHomeAreaTextKey = "Designator_context_harvest_home";
		
		public override string EntryTextKey {
			get { return HarvestAllTextKey; }
		}

		public override string SettingId {
			get { return "providerHarvest"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_PlantsHarvest); }
		}

		protected override ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, "Designator_context_harvestFullyGrown", (des, map) => 
					Find.DesignatorManager.Select(new Designator_HarvestFullyGrown()),
				"Designator_context_fullyGrown_desc", AllowToolDefOf.Textures.designatorSelectionOption);
			yield return MakeMenuOption(designator, HarvestAllTextKey, ContextMenuAction);
			yield return MakeMenuOption(designator, HarvestHomeAreaTextKey, ContextMenuActionInHomeArea);
		}
	}
}