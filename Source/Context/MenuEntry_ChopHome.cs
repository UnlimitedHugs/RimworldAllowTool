using Verse;

namespace AllowTool.Context {
	public class MenuEntry_ChopHome : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "chopHome";
		protected override string BaseTextKey => "Designator_context_chop_home";
		protected override string BaseMessageKey => "Designator_context_chop";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateInHomeArea(designator, map, GetExceptAnimaTreeFilter());
		}
	}
}