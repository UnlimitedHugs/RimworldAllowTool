using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HarvestAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_harvest";
		protected override string SettingHandleSuffix => "harvestAll";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;
	}
}