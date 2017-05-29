using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Adds two menu entries: cancel all designations and cancel all blueprints
	/// </summary>
	public class MenuProvider_Cancel : BaseDesignatorMenuProvider {
		private const string CancelDesignationsTextKey = "Designator_context_cancel_desig";
		private const string CancelBlueprintsTextKey = "Designator_context_cancel_build";

		public override string EntryTextKey {
			get { return "Designator_context_cancel_desig"; }
		}

		public override string SettingId {
			get { return "providerCancel"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Cancel); }
		}

		public override void ContextMenuAction(Designator designator, Map map) {
			RemoveDesignationsAction(designator, map);
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, CancelDesignationsTextKey, RemoveDesignationsAction);
			yield return MakeMenuOption(designator, CancelBlueprintsTextKey, RemoveBlueprintsAction);
		}

		protected void RemoveDesignationsAction(Designator designator, Map map) {
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
			Messages.Message("Designator_context_cancel_desig_msg".Translate(hitCountThings, hitCountTiles), MessageSound.Benefit);
		}
		
		protected void RemoveBlueprintsAction(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var blueprint in map.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).ToArray()) {
				blueprint.Destroy(DestroyMode.Cancel);
				hitCount++;
			}
			ReportActionResult(hitCount, CancelBlueprintsTextKey);
		}
	}
}