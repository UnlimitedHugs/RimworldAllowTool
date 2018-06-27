using System;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// Draws controls, handles input, and provides job driving functionality for the "drafted hunting" tool.
	/// </summary>
	public static class PartyHuntController {
		private const float MaxPartyMemberDistance = 20f;
		private const float MaxFinishOffDistance = 20f;

		// stop attacking and don't target downed animals if auto finish off is enabled
		private static readonly Predicate<Pawn> HuntingTargetAttackFilter = pawn => !pawn.Downed || !AllowToolController.Instance.PartyHuntFinishSetting;
		private static readonly Predicate<Pawn> HuntingTargetFinishFilter = pawn => pawn.Downed && !pawn.HasDesignation(AllowToolDefOf.FinishOffDesignation);

		public static Gizmo TryGetGizmo(Pawn pawn) {
			if (!pawn.Drafted || !AllowToolController.Instance.PartyHuntSetting) return null;
			var toggle = new Command_Toggle {
				defaultLabel = "PartyHuntToggle_label".Translate(),
				defaultDesc = "PartyHuntToggle_desc".Translate(),
				isActive = () => PartyHuntIsEnabled(pawn),
				toggleAction = () => ToggleAction(pawn),
				hotKey = KeyBindingDefOf.Misc9,
				disabled = !AllowToolUtility.PawnCapableOfViolence(pawn)
			};
			if (toggle.disabled) {
				toggle.disabledReason = "IsIncapableOfViolence".Translate(pawn.Name.ToStringShort);
			}
			return toggle;
		}

		private static void ToggleAction(Pawn pawn) {
			AllowToolController.Instance.WorldSettings.TogglePawnPartyHunting(pawn, !PartyHuntIsEnabled(pawn));
		}


		private static bool PartyHuntIsEnabled(Pawn pawn) {
			var settings = AllowToolController.Instance.WorldSettings;
			return settings != null && settings.PawnIsPartyHunting(pawn);
		}

		public static void OnPawnUndrafted(Pawn pawn) {
			AllowToolController.Instance.WorldSettings.TogglePawnPartyHunting(pawn, false);
		}

		public static void DoBehaviorForPawn(JobDriver_Wait driver) {
			var hunter = driver.pawn;
			if (!AllowToolController.Instance.PartyHuntSetting || !PartyHuntIsEnabled(hunter)) return;
			var verb = hunter.TryGetAttackVerb(null, !hunter.IsColonist);
			if (hunter.Faction != null && driver.job.def == JobDefOf.Wait_Combat && AllowToolUtility.PawnCapableOfViolence(hunter) && !hunter.stances.FullBodyBusy) {
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
				if(!hunter.stances.FullBodyBusy && AllowToolController.Instance.PartyHuntFinishSetting && !AnyHuntingPartyMembersInCombat(hunter, MaxPartyMemberDistance)) {
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
						hunter.jobs.jobQueue.EnqueueFirst(new Job(JobDefOf.Goto, hunter.Position));
					}
				}
			}
		}

		private static bool AnyHuntingPartyMembersInCombat(Pawn centerPawn, float maxPartyMemberDistance) {
			return centerPawn.Map.mapPawns.FreeColonists.Where(p => 
				PartyHuntIsEnabled(p) && centerPawn.Position.DistanceTo(p.Position) <= maxPartyMemberDistance
			).Any(p => p.stances.FullBodyBusy);
		}

		private static Pawn TryFindHuntingTarget(Pawn searcher, float minDistance, float maxDistance, Predicate<Pawn> extraPredicate) {
			var minDistanceSquared = minDistance * minDistance;
			var maxDistanceSquared = maxDistance * maxDistance;
			Predicate<Thing> validator = t => {
				var targetPawn = t as Pawn;
				if (targetPawn == null) return false;
				var distanceSquared = (searcher.Position - t.Position).LengthHorizontalSquared;
				if (distanceSquared < minDistanceSquared || distanceSquared > maxDistanceSquared){
					return false;
				}
				if (targetPawn.Position.Fogged(searcher.Map) || !searcher.CanSee(targetPawn)) {
					return false;
				}
				return targetPawn.RaceProps != null && targetPawn.RaceProps.Animal && targetPawn.Faction == null && 
					(!AllowToolController.Instance.PartyHuntDesignatedSetting || targetPawn.HasDesignation(DesignationDefOf.Hunt)) && 
					(extraPredicate == null || extraPredicate(targetPawn));
			};
			int searchRegionsMax = maxDistance <= 800f ? 40 : -1;
			var target = GenClosest.ClosestThingReachable(searcher.Position, searcher.Map, ThingRequest.ForGroup(ThingRequestGroup.AttackTarget),
				PathEndMode.Touch, TraverseParms.For(searcher), maxDistance, validator, null, 0, searchRegionsMax);
			return target as Pawn;
		}

		private static void ResetAutoUndraftTimer(Pawn_DraftController draftController) {
			// resets the expiration timer on the pawn draft
			var undrafter = (AutoUndrafter)AllowToolController.DraftControllerAutoUndrafterField.GetValue(draftController);
			undrafter.Notify_Drafted();
		}
	}
}