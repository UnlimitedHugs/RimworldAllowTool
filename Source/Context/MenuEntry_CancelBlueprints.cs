using Verse;

namespace AllowTool.Context {
	public class MenuEntry_CancelBlueprints : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "cancelBlueprints";
		protected override string BaseTextKey => "Designator_context_cancel_build";

		public override ActivationResult Activate(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var blueprint in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).ToArray()) {
				blueprint.Destroy(DestroyMode.Cancel);
				hitCount++;
			}
			return ActivationResult.FromCount(hitCount, BaseMessageKey);
		}
	}
}