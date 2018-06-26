using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// A regular rearm job, but tied to the rearm urgently designation
	/// </summary>
	public class JobDriver_RearmUrgently : JobDriver {
		private const int RearmTicks = 800;

		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(job.targetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnThingMissingDesignation(TargetIndex.A, AllowToolDefOf.RearmUrgentlyDesignation);
			var toil = new Toil {
				initAction = () => pawn.pather.StartPath(TargetThingA, PathEndMode.Touch),
				defaultCompleteMode = ToilCompleteMode.PatherArrival
			}.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return toil;
			yield return Toils_General.Wait(RearmTicks).WithProgressBarToilDelay(TargetIndex.A);
			yield return new Toil {
				initAction = () => {
					var thing = job.targetA.Thing;
					thing.ToggleDesignation(AllowToolDefOf.RearmUrgentlyDesignation, false);
					var trap = thing as Building_TrapRearmable;
					if(trap != null) trap.Rearm();
					pawn.records.Increment(RecordDefOf.TrapsRearmed);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}
	}
}
