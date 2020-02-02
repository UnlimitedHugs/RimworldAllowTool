using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AllowTool.Context {
	public class MenuEntry_CancelSelected : BaseContextMenuEntry {
		protected override string SettingHandleSuffix => "cancelSelected";
		protected override string BaseTextKey => "Designator_context_cancel_selected";

		public override ActivationResult Activate(Designator designator, Map map) {
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
			return affectedDesignations.Count > 0
				? ActivationResult.Success(BaseMessageKey, selectedDesignationDefs.Length, affectedDesignations.Count)
				: ActivationResult.Failure(BaseMessageKey);
		}
	}
}