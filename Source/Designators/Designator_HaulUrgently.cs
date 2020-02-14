using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Designates things for urgent hauling.
	/// </summary>
	public class Designator_HaulUrgently : Designator_SelectableThings {
		protected override DesignationDef Designation {
			get { return AllowToolDefOf.HaulUrgentlyDesignation; }
		}

		public Designator_HaulUrgently() {
			UseDesignatorDef(AllowToolDefOf.HaulUrgentlyDesignator);
		}

		protected override void FinalizeDesignationSucceeded() {
			base.FinalizeDesignationSucceeded();
			if (HugsLibUtility.ShiftIsHeld) {
				foreach (var colonist in Find.CurrentMap.mapPawns.FreeColonists) {
					colonist.jobs.CheckForJobOverride();
				}
			}
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			return ThingIsRelevant(t) && !t.HasDesignation(AllowToolDefOf.HaulUrgentlyDesignation);
		}

		public override void DesignateThing(Thing thing) {
			if (thing.def.designateHaulable) {
				// for things that require explicit hauling designation, such as rock chunks
				thing.ToggleDesignation(DesignationDefOf.Haul, true);
			}
			thing.ToggleDesignation(AllowToolDefOf.HaulUrgentlyDesignation, true);
			// unforbid for convenience
			thing.SetForbidden(false, false);
		}

		private bool ThingIsRelevant(Thing thing) {
			if (thing.def == null || thing.Position.Fogged(thing.Map)) return false;
			return (thing.def.alwaysHaulable || thing.def.EverHaulable) && !thing.IsInValidBestStorage();
		}
	}
}