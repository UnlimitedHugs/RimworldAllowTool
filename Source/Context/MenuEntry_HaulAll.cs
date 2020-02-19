using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HaulAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_haul";
		protected override string SettingHandleSuffix => "haulAll";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.HaulableEver;
	}
}