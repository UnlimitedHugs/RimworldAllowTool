using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Marks downed pawns for killing
	/// </summary>
	public class MenuProvider_FinishOff : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_finish"; }
		}

		public override string SettingId {
			get { return "providerFinish"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_FinishOff); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Pawn; }
		}

		public override void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			bool friendliesFound = false;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (ValidForDesignation(thing) && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
					if (AllowToolUtility.PawnIsFriendly(thing)) {
						friendliesFound = true;
					}
				}
			}
			if (hitCount>0 && friendliesFound) {
				Messages.Message("Designator_context_finish_allies".Translate(hitCount), MessageTypeDefOf.CautionInput);
			}
			ReportActionResult(hitCount);
		}
	}
}