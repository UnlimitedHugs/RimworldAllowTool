using System;
using System.Collections.Generic;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_HarvestFullyGrown : BaseDesignatorMenuProvider {
		private const string HarvestAllTextKey = "Designator_context_harvest_fullgrown";
		private const string HarvestHomeAreaTextKey = "Designator_context_harvest_home_fullgrown";

		public override string EntryTextKey {
			get { return HarvestAllTextKey; }
		}

		public override string SettingId {
			get { return "providerHarvestFullyGrown"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_HarvestFullyGrown); }
		}

		protected override ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, HarvestAllTextKey, ContextMenuAction);
			yield return MakeMenuOption(designator, HarvestHomeAreaTextKey, ContextMenuActionInHomeArea);
		}
	}
}