using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Adds two menu entries: cancel all designations and cancel all blueprints
	/// </summary>
	public class MenuProvider_Cancel : BaseDesignatorMenuProvider {
		private const string CancelSelectedDesignationTextKey = "Designator_context_cancel_selected";
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
			RemoveSelectedDesignation(designator, map);
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, CancelSelectedDesignationTextKey, RemoveSelectedDesignation);
			yield return MakeMenuOption(designator, CancelDesignationsTextKey, RemoveDesignationsAction);
			yield return MakeMenuOption(designator, CancelBlueprintsTextKey, RemoveBlueprintsAction);
		}

		private void RemoveSelectedDesignation(Designator designator, Map map) {
			// distinct designation defs on selected things
			var selectedObjects = new HashSet<object>(Find.Selector.SelectedObjects);
			// also include designations on cells of selected things
			var selectedTilePositions = new HashSet<IntVec3>(
				selectedObjects.Where(t => t is Thing)
					.Select(t => ((Thing)t).Position)
				);
			var selectedDesignationDefs = map.designationManager.allDesignations
				.Where(des => des.target.HasThing ? selectedObjects.Contains(des.target.Thing) : selectedTilePositions.Contains(des.target.Cell))
				.Select(des => des.def)
				.Distinct()
				.ToArray();
			var affectedDesignations = new HashSet<LocalTargetInfo>();
			foreach (var designation in map.designationManager.allDesignations.ToArray()) {
				if (selectedDesignationDefs.Contains(designation.def)) {
					map.designationManager.RemoveDesignation(designation);
					affectedDesignations.Add(designation.target);
				}
			}
			if (affectedDesignations.Count > 0) {
				Messages.Message((CancelSelectedDesignationTextKey + SuccessMessageStringIdSuffix).Translate(selectedDesignationDefs.Length, affectedDesignations.Count), 
					MessageTypeDefOf.TaskCompletion);
			} else {
				Messages.Message((CancelSelectedDesignationTextKey + FailureMessageStringIdSuffix).Translate(), MessageTypeDefOf.RejectInput);
			}
		}

		private void RemoveDesignationsAction(Designator designator, Map map) {
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
			Messages.Message("Designator_context_cancel_desig_msg".Translate(hitCountThings, hitCountTiles), MessageTypeDefOf.TaskCompletion);
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