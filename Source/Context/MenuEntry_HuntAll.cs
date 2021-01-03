using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HuntAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_hunt";
		protected override string SettingHandleSuffix => "huntAll";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.Pawn;
	}
}