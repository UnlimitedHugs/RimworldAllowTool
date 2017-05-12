using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Hunt : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_hunt"; }
		}

		public override string SettingId {
			get { return "providerHunt"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Hunt); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Pawn; }
		}
	}
}