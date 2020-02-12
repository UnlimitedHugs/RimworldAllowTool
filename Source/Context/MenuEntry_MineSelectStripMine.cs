using Verse;

namespace AllowTool.Context {
	public class MenuEntry_MineSelectStripMine : BaseContextMenuEntry {
		protected override string BaseTextKey => "Designator_context_mine_selectStripMine";
		protected override string SettingHandleSuffix => "mineSelectStripMine";

		public override FloatMenuOption MakeMenuOption(Designator designator) {
			return MakeStandardOption(designator, null, AllowToolDefOf.Textures.designatorSelectionOption);
		}

		public override ActivationResult Activate(Designator designator, Map map) {
			Find.DesignatorManager.Select(new Designator_StripMine());
			return new ActivationResult();
		}
	}
}