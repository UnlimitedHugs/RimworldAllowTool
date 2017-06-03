using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// Assigns pawns the JobDriver_FinishOff job
	/// </summary>
	public class WorkGiver_FinishOff : WorkGiver_Scanner {
		private static bool WorkGiverEnabled {
			get {
				return AllowToolController.Instance.IsDesignatorEnabledInSettings(AllowToolDefOf.FinishOffDesignator)
						|| AllowToolController.Instance.IsReverseDesignatorEnabledInSettings(AllowToolDefOf.ReverseFinishOff);
			}
		}

		// this allow the work giver to be present in both drafted and undrafted float menus
		public static FloatMenuOption InjectThingFloatOptionIfNeeded(Thing target, Pawn selPawn) {
			if (Designator_FinishOff.IsValidDesignationTarget(target)) {
				if (WorkGiverEnabled) {
					JobFailReason.Clear();
					var giver = new WorkGiver_FinishOff {def = AllowToolDefOf.FinishOff};
					var job = giver.JobOnThing(selPawn, target, true);
					var opt = new FloatMenuOption("Finish_off_floatMenu".Translate(target.LabelShort), () => {
						selPawn.jobs.TryTakeOrderedJobPrioritizedWork(job, giver, target.Position);
					});
					opt = FloatMenuUtility.DecoratePrioritizedTask(opt, selPawn, target);
					if (job == null) {
						opt.Disabled = true;
						if (JobFailReason.HaveReason) {
							opt.Label = "CannotGenericWork".Translate(giver.def.verb, target.LabelShort) + " (" + JobFailReason.Reason + ")";
						}
					}
					return opt;
				}
			}
			return null;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			if (Designator_FinishOff.IsValidDesignationTarget(t)) {
				if (pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)) {
					var friendlyReport = Designator_FinishOff.FriendlyPawnIsValidTarget(t);
					if (forced && !friendlyReport.Accepted) {
						// ignore if not forced- only designated targets will be picked in this case
						JobFailReason.Is(friendlyReport.Reason);
					} else {
						var skillReport = Designator_FinishOff.PawnMeetsSkillRequirement(pawn);
						if (!skillReport.Accepted) {
							JobFailReason.Is(skillReport.Reason);
						} else {
							if (forced || t.HasDesignation(AllowToolDefOf.FinishOffDesignation)) {
								// ignore designation if forced- driver will add the designation
								var verb = pawn.meleeVerbs != null ? pawn.meleeVerbs.TryGetMeleeVerb() : null;
								if (verb != null) {
									return new Job(AllowToolDefOf.FinishOffPawn, t) {
										verbToUse = verb,
										killIncappedTarget = true
									};
								}
							}
						}
					}
				}

			}
			return null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			// enumerate all downed pawns to allow forcing the job without a designation
			if (WorkGiverEnabled) {
				var pawns = pawn.Map.mapPawns.AllPawnsSpawned;
				for (int i = 0; i < pawns.Count; i++) {
					var target = pawns[i];
					if (Designator_FinishOff.IsValidDesignationTarget(target)) {
						yield return target;
					}
				}
			}
		}
	}
}