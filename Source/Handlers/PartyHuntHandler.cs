using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// Draws controls, handles input, and provides job driving functionality for the "drafted hunting" tool.
	/// </summary>
	public static class PartyHuntHandler {
		private const float MaxPartyMemberDistance = 20f;
		private const float MaxFinishOffDistance = 20f;

		private delegate bool HuntingTargetFilter(Pawn target, Pawn hunter);

		// stop attacking and don't target downed animals if auto finish off is enabled
		private static readonly HuntingTargetFilter HuntingTargetAttackFilter = 
			(target, hunter) => !target.Downed || (CanDoCommonerWork(hunter) && !WorldSettings.AutoFinishOff);
		private static readonly HuntingTargetFilter HuntingTargetFinishFilter = 
			(target, _) => target.Downed && !target.HasDesignation(AllowToolDefOf.FinishOffDesignation);
		private static readonly List<HuntingTargetCandidate> huntingTargetCandidates = new List<HuntingTargetCandidate>();

		private static PartyHuntSettings WorldSettings {
			get { return AllowToolController.Instance.WorldSettings.PartyHunt; }
		}

		public static Gizmo TryGetGizmo(Pawn pawn) {
			if (pawn.Name == null || !pawn.Drafted || !AllowToolController.Instance.Handles.PartyHuntSetting) return null;
			return new Command_PartyHunt(pawn);
		}

		public static void OnPawnUndrafted(Pawn pawn) {
			WorldSettings.TogglePawnPartyHunting(pawn, false);
		}

		public static void DoBehaviorForPawn(JobDriver_Wait driver) {
			var hunter = driver.pawn;
			if (!AllowToolController.Instance.Handles.PartyHuntSetting || !WorldSettings.PawnIsPartyHunting(hunter)) return;
			var verb = hunter.TryGetAttackVerb(null, !hunter.IsColonist);
			if (hunter.Faction != null 
				&& driver.job.def == JobDefOf.Wait_Combat 
				&& AllowToolUtility.PawnCapableOfViolence(hunter) 
				&& !hunter.stances.FullBodyBusy) {
				// fire at target
				if (hunter.drafter.FireAtWill) {
					// fudge melee range for easier target acquisition
					var weaponRange = verb.verbProps.IsMeleeAttack ? 2 : verb.verbProps.range;
					var target = TryFindHuntingTarget(hunter, verb.verbProps.minRange, weaponRange, HuntingTargetAttackFilter);
					if (target != null) {
						hunter.TryStartAttack(target);
						ResetAutoUndraftTimer(hunter.drafter);
					}
				}
				// finish off targets. Wait for everyone to finish firing to avoid catching stray bullets
				if(!hunter.stances.FullBodyBusy && WorldSettings.AutoFinishOff 
					&& CanDoCommonerWork(hunter) && !AnyHuntingPartyMembersInCombat(hunter, MaxPartyMemberDistance)) {
					// try mark a downed animal
					var target = TryFindHuntingTarget(hunter, 0, MaxFinishOffDistance, HuntingTargetFinishFilter);
					if (target != null) {
						target.ToggleDesignation(AllowToolDefOf.FinishOffDesignation, true);
					}
					// query work giver for finish off job in range
					var job = WorkGiver_FinishOff.CreateInstance().TryGetJobInRange(hunter, MaxFinishOffDistance);
					if (job != null) {
						hunter.jobs.StartJob(job, JobCondition.Ongoing, null, true);
						// return to starting position
						hunter.jobs.jobQueue.EnqueueFirst(JobMaker.MakeJob(JobDefOf.Goto, hunter.Position));
					}
				}
			}
		}

		private static bool AnyHuntingPartyMembersInCombat(Pawn centerPawn, float maxPartyMemberDistance) {
			return centerPawn.Map.mapPawns.FreeColonists.Where(
				p => WorldSettings.PawnIsPartyHunting(p) && centerPawn.Position.DistanceTo(p.Position) <= maxPartyMemberDistance
			).Any(p => p.stances.FullBodyBusy);
		}

		private static Pawn TryFindHuntingTarget(Pawn searcher, float minDistance, float maxDistance, HuntingTargetFilter targetFilter) {
			var minDistanceSquared = minDistance * minDistance;
			var maxDistanceSquared = maxDistance * maxDistance;

			bool validator(Pawn pawn) {
				if (pawn == null) return false;
				var distanceSquared = (searcher.Position - pawn.Position).LengthHorizontalSquared;
				if (distanceSquared < minDistanceSquared || distanceSquared > maxDistanceSquared) {
					return false;
				}
				if (pawn.Position.Fogged(searcher.Map) || !searcher.CanSee(pawn)) {
					return false;
				}
				return pawn.RaceProps != null
						&& pawn.RaceProps.Animal
						&& pawn.Faction == null
						&& (!WorldSettings.HuntDesignatedOnly || pawn.HasDesignation(DesignationDefOf.Hunt))
						&& (targetFilter == null || targetFilter(pawn, searcher));
			}

			huntingTargetCandidates.Clear();
			var mapPawns = searcher.Map.mapPawns.AllPawnsSpawned;
			for (var i = 0; i < mapPawns.Count; i++) {
				var pawn = mapPawns[i];
				if (validator(pawn)) {
					huntingTargetCandidates.Add(new HuntingTargetCandidate(pawn, (searcher.Position - pawn.Position).LengthHorizontalSquared));
				}
			}
			huntingTargetCandidates.Sort();
			return huntingTargetCandidates.Count > 0 ? huntingTargetCandidates[0].target : null;
		}

		private static void ResetAutoUndraftTimer(Pawn_DraftController draftController) {
			// resets the expiration timer on the pawn draft
			var undrafter = (AutoUndrafter)AllowToolController.Instance.Reflection.DraftControllerAutoUndrafterField.GetValue(draftController);
			undrafter.Notify_Drafted();
		}

		private static bool CanDoCommonerWork(Pawn pawn) {
			return !pawn.WorkTagIsDisabled(WorkTags.Commoner);
		}

		// provides a more efficient way to sort hunting targets by distance
		private readonly struct HuntingTargetCandidate : IComparable<HuntingTargetCandidate> {
			public readonly Pawn target;
			private readonly int distanceSquared;

			public HuntingTargetCandidate(Pawn target, int distanceSquared) {
				this.target = target;
				this.distanceSquared = distanceSquared;
			}

			public int CompareTo(HuntingTargetCandidate other) {
				return distanceSquared.CompareTo(other.distanceSquared);
			}
		}
	}
}