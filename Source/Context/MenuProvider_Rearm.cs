using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Rearm : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_rearm"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_RearmTrap); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.BuildingArtificial; }
		}
	}
}