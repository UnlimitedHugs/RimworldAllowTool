using Verse;

namespace AllowTool.Context {
	public class MenuEntry_ForbidVisible : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "forbidVisible";
		protected override string BaseTextKey => "Designator_context_forbid_visible";

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateInVisibleArea(designator, map);
		}
	}
}