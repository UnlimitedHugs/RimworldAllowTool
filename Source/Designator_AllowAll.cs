using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AllowTool {
	public class Designator_AllowAll : Designator_SelectableThings {
		public Designator_AllowAll(ThingDesignatorDef def) : base(def) {
		}

		public override void ProcessInput(Event ev) {
			//if (!CheckCanInteract()) return;
			AllowAllTheThings();
		}

		protected override bool ThingIsRelevant(Thing item) {
			return false;
		}

		protected override int ProcessCell(IntVec3 cell) {
			return 0;
		}

		private void AllowAllTheThings() {
			var includeRotten = AllowToolUtility.ShiftIsHeld;
			var things = Find.ListerThings.AllThings;
			var tallyCount = 0;
			for (var i = 0; i < things.Count; i++) {
				var thing = things[i];
				var comp = thing is ThingWithComps ? (thing as ThingWithComps).GetComp<CompForbiddable>() : null;
				if (comp != null && comp.Forbidden) {
					CompRottable rottable;
					if (includeRotten || !(thing is Corpse) || (rottable = (thing as ThingWithComps).GetComp<CompRottable>()) == null || rottable.Stage < RotStage.Rotting) {
						comp.Forbidden = false;
						tallyCount++;
					}
				}
			}
			if (tallyCount > 0) {
				if (def.messageSuccess != null) Messages.Message(def.messageSuccess.Translate(tallyCount.ToString()), MessageSound.Silent);
				def.soundSucceded.PlayOneShotOnCamera();
			} else {
				if (def.messageFailure != null) Messages.Message(def.messageFailure.Translate(), MessageSound.RejectInput);
			}
		}
	}
}