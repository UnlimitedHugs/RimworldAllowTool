using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_CancelDesignations : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "cancelDesginations";
		protected override string BaseTextKey => "Designator_context_cancel_desig";

		public override ActivationResult Activate(Designator designator, Map map) {
			int hitCountThings = 0;
			int hitCountTiles = 0;
			var manager = map.designationManager;
			foreach (var des in manager.allDesignations.ToArray()) {
				// skip planning designation, as so does cancel
				if (des.def == null || !des.def.designateCancelable || des.def == DesignationDefOf.Plan) continue;
				if (des.target.Thing != null) {
					hitCountThings++;
				} else {
					hitCountTiles++;
				}
				manager.RemoveDesignation(des);
			}
			return ActivationResult.SuccessMessage("Designator_context_cancel_desig_msg".Translate(hitCountThings, hitCountTiles));
		}
	}
}