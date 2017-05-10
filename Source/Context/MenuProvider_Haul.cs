using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Haul : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_haul"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Haul); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.HaulableEver; }
		}
	}
}