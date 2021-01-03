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
		private static readonly Pawn[] emptyPawnsArray = new Pawn[0];

		private static bool WorkGiverEnabled {
			get {
				return AllowToolController.Instance.Handles.IsDesignatorEnabled(AllowToolDefOf.FinishOffDesignator)
						|| AllowToolController.Instance.Handles.IsReverseDesignatorEnabled(AllowToolDefOf.ReverseFinishOff);
			}
		}

		// this allow the work giver to be present in both drafted and undrafted float menus
		public static FloatMenuOption InjectThingFloatOptionIfNeeded(Thing target, Pawn selPawn) {
			if (Designator_FinishOff.IsValidDesignationTarget(target)) {
				if (WorkGiverEnabled) {
					JobFailReason.Clear();
					var giver = CreateInstance();
					var job = giver.JobOnThing(selPawn, target, true);
					var opt = new FloatMenuOption("Finish_off_floatMenu".Translate(target.LabelShort), () => {
						selPawn.jobs.TryTakeOrderedJobPrioritizedWork(job, giver, target.Position);
					});
					opt = FloatMenuUtility.DecoratePrioritizedTask(opt, selPawn, target);
					if (job == null) {
						opt.Disabled = true;
						if (JobFailReason.HaveReason) {
							opt.Label = "CannotGenericWork".Translate(giver.def.verb, target.LabelShort, target) + " (" + JobFailReason.Reason + ")";
						}
					}
					return opt;
				}
			}
			return null;
		}

		public static WorkGiver_FinishOff CreateInstance() {
			return new WorkGiver_FinishOff {def = AllowToolDefOf.FinishOff};
		}

		public Job TryGetJobInRange(Pawn pawn, float maxRange) {
			var rangeSquared = maxRange * maxRange;
			foreach (var target in GetPotentialTargets(pawn)) {
				if (pawn.Position.DistanceToSquared(target.Position) < rangeSquared) {
					var job = JobOnThing(pawn, target);
					if (job != null) return job;
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
						var skillReport = Designator_FinishOff.PawnMeetsSkillRequirement(pawn, t as Pawn);
						if (!skillReport.Accepted) {
							JobFailReason.Is(skillReport.Reason);
						} else {
							if (forced || t.HasDesignation(AllowToolDefOf.FinishOffDesignation)) {
								// ignore designation if forced- driver will add the designation
								var verb = pawn.meleeVerbs?.TryGetMeleeVerb(t);
								if (verb != null) {
									var job = JobMaker.MakeJob(AllowToolDefOf.FinishOffPawn, t);
									job.verbToUse = verb;
									job.killIncappedTarget = true;
									return job;
								}
							}
						}
					}
				}

			}
			return null;
		}

		public override ThingRequest PotentialWorkThingRequest {
			get { return ThingRequest.ForGroup(ThingRequestGroup.Pawn); }
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			if (WorkGiverEnabled) {
				return GetPotentialTargets(pawn);
			}
			return emptyPawnsArray;
		}

		private IEnumerable<Thing> GetPotentialTargets(Pawn pawn) {
			// enumerate all downed pawns to allow forcing the job without a designation
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