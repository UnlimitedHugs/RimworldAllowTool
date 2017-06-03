using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AllowTool {
	
	/// <summary>
	/// Unforbids all forbidden things on the map.
	/// Holding Shift will include rotten remains.
	/// </summary>
	public class Designator_AllowAll : Designator_SelectableThings {
		public Designator_AllowAll(ThingDesignatorDef def) : base(def) {
		} 

		public override void Selected() {
			Find.DesignatorManager.Deselect();
			if (!CheckCanInteract()) return;
			AllowAllTheThings();
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			return false;
		}

		public override void DesignateSingleCell(IntVec3 cell) {
			numThingsDesignated = 0;
		}

		private void AllowAllTheThings() {
			var includeRotten = HugsLibUtility.ShiftIsHeld;
			var includeNonHaulable = HugsLibUtility.ControlIsHeld;
			var map = Find.VisibleMap;
			if(map == null) return;
			var things = Find.VisibleMap.listerThings.AllThings;
			var tallyCount = 0;
			for (var i = 0; i < things.Count; i++) {
				var thing = things[i];
				var comp = thing is ThingWithComps ? (thing as ThingWithComps).GetComp<CompForbiddable>() : null;
				var thingCellFogged = map.fogGrid.IsFogged(thing.Position);
				if (comp != null && !thingCellFogged && comp.Forbidden && (includeNonHaulable || (thing.def != null && thing.def.EverHaulable))) {
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