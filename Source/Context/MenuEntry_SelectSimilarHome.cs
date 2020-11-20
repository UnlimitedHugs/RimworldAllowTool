using Verse;

namespace AllowTool.Context {
	public class MenuEntry_SelectSimilarHome : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_similar_home";
		protected override string BaseMessageKey => "Designator_context_similar";
		protected override string SettingHandleSuffix => "selectSimilarHome";

		public override ActivationResult Activate(Designator designator, Map map) {
			return MenuEntry_SelectSimilarAll.SelectSimilarWithFilter(designator, map,
				BaseTextKey, BaseMessageKey, GetHomeAreaFilter(map));
		}
	}
}