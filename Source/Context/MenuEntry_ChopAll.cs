using Verse;

namespace AllowTool.Context {
	public class MenuEntry_ChopAll : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "chopAll";
		protected override string BaseTextKey => "Designator_context_chop";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Plant;

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateWithFilter(designator, map, GetExceptAnimaTreeFilter());
		}
	}
}