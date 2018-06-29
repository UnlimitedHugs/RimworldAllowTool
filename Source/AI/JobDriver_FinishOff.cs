using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace AllowTool {
	/// <summary>
	/// Walk up to and murderize a downed pawn with a fancy effect
	/// </summary>
	public class JobDriver_FinishOff : JobDriver {
		private const int PrepareSwingDuration = 60;
		private const float VictimSkullMoteChance = .25f;
		private const float OpportunityTargetMaxRange = 8f;

		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job);
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			if (job.playerForced) {
				TargetA.Thing.ToggleDesignation(AllowToolDefOf.FinishOffDesignation, true);
			}
			AddFailCondition(JobHasFailed);
			yield return Toils_Misc.ThrowColonistAttackingMote(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Thing skullMote = null;
			yield return new Toil {
				initAction = () => {
					var victim = job.targetA.Thing as Pawn;
					skullMote = TryMakeSkullMote(victim, VictimSkullMoteChance);
					AllowToolDefOf.EffecterWeaponGlint.Spawn().Trigger(pawn, job.targetA.Thing);
				},
				defaultDuration = PrepareSwingDuration,
				defaultCompleteMode = ToilCompleteMode.Delay
			};
			yield return new Toil {
				initAction = () => {
					var victim = job.targetA.Thing as Pawn;
					if (victim == null || job.verbToUse == null) return;
					job.verbToUse.TryStartCastOn(victim);
					DoSocialImpact(victim);
					DoExecution(pawn, victim);
					if (skullMote != null && !skullMote.Destroyed) {
						skullMote.Destroy();
					}
					// look for a target of opportunity nearby before moving on
					// needed by drafted hunters. Their finish off job was not work-related, so they need to be fed a new opportunity job manually
					if (!job.playerForced) {
						var opportunityJob = WorkGiver_FinishOff.CreateInstance().TryGetJobInRange(pawn, OpportunityTargetMaxRange);
						if (opportunityJob != null) {
							pawn.jobs.jobQueue.EnqueueFirst(opportunityJob);
						}
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		private void DoExecution(Pawn slayer, Pawn victim) {
			// swiped from ExecutionUtility
			var position = victim.Position;
			int num = Mathf.Max(GenMath.RoundRandom(victim.BodySize * 8f), 1);
			for (int i = 0; i < num; i++) {
				victim.health.DropBloodFilth();
			}
			var part = victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).FirstOrDefault();
			int amount = part != null ? Mathf.Clamp((int)victim.health.hediffSet.GetPartHealth(part) - 1, 1, 20) : 20;
			var damageInfo = new DamageInfo(DamageDefOf.ExecutionCut, amount, -1f, -1F, slayer, part);
			victim.TakeDamage(damageInfo);
			if (!victim.Dead) {
				victim.Kill(damageInfo);
			}
			var body = position.GetThingList(Map).FirstOrDefault(t => t is Corpse && (t as Corpse).InnerPawn == victim);
			if (body != null) {
				body.SetForbiddenIfOutsideHomeArea();
			}
			if (AllowToolController.Instance.FinishOffUnforbidsSetting) {
				UnforbidAdjacentThingsTo(position, Map);
			}
		}

		private void DoSocialImpact(Pawn victim) {
			var isPrisoner = victim.IsPrisonerOfColony;
			var giveThought = AllowToolUtility.PawnIsFriendly(victim);
			if (giveThought) {
				ThoughtUtility.GiveThoughtsForPawnExecuted(victim, PawnExecutionKind.GenericBrutal);
			}
			if (victim.RaceProps != null && victim.RaceProps.intelligence == Intelligence.Animal) {
				pawn.records.Increment(RecordDefOf.AnimalsSlaughtered);
			}
			if (isPrisoner) {
				TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, victim);
			}
		}

		private Thing TryMakeSkullMote(Pawn victim, float chance) {
			if (victim != null && victim.RaceProps != null && victim.RaceProps.intelligence == Intelligence.Humanlike) {
				if (Rand.Chance(chance)) {
					var def = ThingDefOf.Mote_ThoughtGood;
					var moteBubble = (MoteBubble)ThingMaker.MakeThing(def);
					moteBubble.SetupMoteBubble(ThoughtDefOf.WitnessedDeathAlly.Icon, null);
					moteBubble.Attach(victim);
					return GenSpawn.Spawn(moteBubble, victim.Position, victim.Map);
				}
			}
			return null;
		}

		private bool JobHasFailed() {
			var target = TargetThingA as Pawn;
			return target == null || !target.Spawned || target.Dead || !target.Downed || !target.HasDesignation(AllowToolDefOf.FinishOffDesignation);
		}

		private void UnforbidAdjacentThingsTo(IntVec3 center, Map map) {
			foreach (var offset in GenAdj.AdjacentCellsAndInside) {
				var pos = center + offset;
				if (pos.InBounds(map)) {
					AllowToolUtility.ToggleForbiddenInCell(pos, map, false);
				}
			}
		}
	}
}