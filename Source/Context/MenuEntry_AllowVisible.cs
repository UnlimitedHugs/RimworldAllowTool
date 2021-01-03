using Verse;

namespace AllowTool.Context {
	public class MenuEntry_AllowVisible : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "allowVisible";
		protected override string BaseTextKey => "Designator_context_allow_visible";

		public override ActivationResult Activate(Designator designator, Map map) {
			return ActivateInVisibleArea(designator, map);
		}
	}
}