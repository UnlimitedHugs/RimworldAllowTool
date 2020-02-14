using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.Sound;

namespace AllowTool {
	/// <summary>
	/// Unforbids all forbidden things on the map.
	/// Holding Shift will include rotten remains.
	/// </summary>
	public class Designator_AllowAll : Designator_DefBased {
		public Designator_AllowAll() {
			UseDesignatorDef(AllowToolDefOf.AllowAllDesignator);
		} 

		public override void Selected() {
			Find.DesignatorManager.Deselect();
			if (!CheckCanInteract()) return;
			AllowAllTheThings();
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			return false;
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc) {
			return false;
		}

		private void AllowAllTheThings() {
			var includeRotten = HugsLibUtility.ShiftIsHeld;
			var includeNonHaulable = HugsLibUtility.ControlIsHeld;
			var map = Find.CurrentMap;
			if(map == null) return;
			var things = Find.CurrentMap.listerThings.AllThings;
			var tallyCount = 0;
			for (var i = 0; i < things.Count; i++) {
				var thing = things[i];
				var comp = (thing as ThingWithComps)?.GetComp<CompForbiddable>();
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
				if (Def.messageSuccess != null) Messages.Message(Def.messageSuccess.Translate(tallyCount.ToString()), MessageTypeDefOf.SilentInput);
				Def.soundSucceeded.PlayOneShotOnCamera();
			} else {
				if (Def.messageFailure != null) Messages.Message(Def.messageFailure.Translate(), MessageTypeDefOf.RejectInput);
			}
		}
	}
}