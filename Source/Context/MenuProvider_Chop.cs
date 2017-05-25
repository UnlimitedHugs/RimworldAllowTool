using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Chop : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_chop"; }
		}

		public override string SettingId {
			get { return "providerChop"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_PlantsHarvestWood); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}
	}
}