using Verse;

namespace AllowTool.Context {
	public class MenuEntry_HaulUrgentAll : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_urgent_all";
		protected override string SettingHandleSuffix => "haulUrgentAll";
		protected override ThingRequestGroup DesignationRequestGroup => ThingRequestGroup.HaulableEver;
		protected override string BaseMessageKey => "Designator_context_urgent";

		public override ActivationResult Activate(Designator designator, Map map) {
			var hitCount = DesignateAllThings(designator, map, CanAutoDesignateThingForUrgentHauling);
			return ActivationResult.FromCount(hitCount, BaseMessageKey);
		}

		internal static bool CanAutoDesignateThingForUrgentHauling(Thing t) {
			return !t.def.designateHaulable;
		}
	}
}