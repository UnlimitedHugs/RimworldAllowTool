/*using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Mine : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_mine"; }
		}

		public override string SettingId {
			get { return "providerMine"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Mine); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Everything; }
		}

		public override bool Enabled {
			get { return AllowToolController.Instance.MineConnectedSetting.Value; }
		}
	}
}*/