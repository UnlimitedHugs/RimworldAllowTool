using System;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_HaulUrgently : BaseDesignatorMenuProvider {
		protected override string EntryTextKey {
			get { return "Designator_context_urgent"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_HaulUrgently); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.HaulableEver; }
		}

		// skip rock chunks in designation
		public override void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (designator.CanDesignateThing(thing).Accepted && !thing.def.designateHaulable) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}
	}
}