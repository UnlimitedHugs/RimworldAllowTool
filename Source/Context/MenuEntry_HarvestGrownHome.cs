using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HarvestGrownHome : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_harvest_home_fullgrown";
		protected override string BaseMessageKey => "Designator_context_harvest_fullgrown";
		protected override string SettingHandleSuffix => "harvestGrownHome";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateInHomeArea(designator, map, GetExceptAnimaTreeFilter());
		}
	}
}