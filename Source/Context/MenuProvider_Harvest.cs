using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Harvest : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_harvest"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_PlantsHarvest); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}
	}
}