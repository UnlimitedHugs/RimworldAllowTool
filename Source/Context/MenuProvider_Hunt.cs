using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Hunt : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_hunt"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Hunt); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Pawn; }
		}
	}
}