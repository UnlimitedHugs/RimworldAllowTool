using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Harvest : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_harvest"; }
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
	}
}