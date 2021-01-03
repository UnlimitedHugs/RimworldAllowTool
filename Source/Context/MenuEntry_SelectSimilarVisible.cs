using Verse;

namespace AllowTool.Context {
	public class MenuEntry_SelectSimilarVisible : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_similar_visible";
		protected override string BaseMessageKey => "Designator_context_similar";
		protected override string SettingHandleSuffix => "selectSimilarVisible";

		public override ActivationResult Activate(Designator designator, Map map) {
			return MenuEntry_SelectSimilarAll.SelectSimilarWithFilter(designator, map, 
				BaseTextKey, BaseMessageKey, GetVisibleThingFilter());
		}
	}
}