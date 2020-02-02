using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HarvestHome : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_harvest_home";
		protected override string BaseMessageKey => "Designator_context_harvest";
		protected override string SettingHandleSuffix => "harvestHome";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateInHomeArea(designator, map);
		}
	}
}