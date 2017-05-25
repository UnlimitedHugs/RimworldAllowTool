using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Rearm : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_rearm"; }
		}

		public override string SettingId {
			get { return "providerRearm"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_RearmTrap); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.BuildingArtificial; }
		}
	}
}