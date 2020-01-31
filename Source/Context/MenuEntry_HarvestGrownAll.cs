using System;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HarvestGrownAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_harvest_fullgrown";
		protected override string SettingHandleSuffix => "harvestGrownAll";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;
		public override Type HandledDesignatorType => typeof(Designator_HarvestFullyGrown);
	}
}