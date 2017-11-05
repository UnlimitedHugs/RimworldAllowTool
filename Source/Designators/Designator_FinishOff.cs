using System;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Designator_FinishOff : Designator_SelectableThings {
		private const int MeleeSkillLevelRequired = 6;
		private const float PawnRecheckIntervalSeconds = 1;

		private float lastPawnCheckTime;

		public static bool IsValidDesignationTarget(Thing t) {
			var p = t as Pawn;
			return p != null && p.def != null && !p.Dead && p.Downed;
		}

		public static AcceptanceReport PawnMeetsSkillRequirement(Pawn pawn) {
			var result = pawn != null && pawn.skills != null && (!AllowToolController.Instance.FinishOffSkillRequirement || pawn.skills.GetSkill(SkillDefOf.Melee).Level >= MeleeSkillLevelRequired);
			return result ? true : new AcceptanceReport("Finish_off_pawnSkillRequired".Translate(MeleeSkillLevelRequired));
		}

		public static AcceptanceReport FriendlyPawnIsValidTarget(Thing t) {
			var result = !AllowToolUtility.PawnIsFriendly(t) || HugsLibUtility.ShiftIsHeld;
			return result ? true : new AcceptanceReport("Finish_off_floatMenu_reason_friendly".Translate());
		}

		public override string Desc {
			get {
				if (AllowToolController.Instance.FinishOffSkillRequirement) {
					return String.Format("{0}\n\n{1}", base.Desc, "Finish_off_skillRequired".Translate(MeleeSkillLevelRequired));
				}
				return base.Desc;
			}
		}

		private bool _anyPawnsMeetSkillRequirement;
		private bool AnyPawnsMeetSkillRequirement {
			get {
				RecacheSkilledPawnAvailabilityIfNeeded();
				return _anyPawnsMeetSkillRequirement;
			}
		}

		public Designator_FinishOff(ThingDesignatorDef def) : base(def) {
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft) {
			UpdateDisabledState();
			return base.GizmoOnGUI(topLeft);
		}

		public override AcceptanceReport CanDesignateThing(Thing t) {
			if (!IsValidDesignationTarget(t) || t.HasDesignation(AllowToolDefOf.FinishOffDesignation)) return false;
			return FriendlyPawnIsValidTarget(t);
		}

		public override void DesignateThing(Thing t) {
			if (!CanDesignateThing(t).Accepted) return;
			t.ToggleDesignation(AllowToolDefOf.FinishOffDesignation, true);
		}

		// for the reverse designator, since we can't disable it
		protected override void FinalizeDesignationSucceeded() {
			base.FinalizeDesignationSucceeded();
			if (!AnyPawnsMeetSkillRequirement) {
				Messages.Message("Finish_off_skillRequired".Translate(MeleeSkillLevelRequired), MessageTypeDefOf.RejectInput);
			}
		}

		private void RecacheSkilledPawnAvailabilityIfNeeded() {
			if (lastPawnCheckTime + PawnRecheckIntervalSeconds < Time.time) {
				lastPawnCheckTime = Time.time;
				var map = Find.VisibleMap;
				_anyPawnsMeetSkillRequirement = false;
				if (map == null) return;
				foreach (var pawn in map.mapPawns.FreeColonists) {
					if (PawnMeetsSkillRequirement(pawn).Accepted) {
						_anyPawnsMeetSkillRequirement = true;
						break;
					}
				}
			}
		}

		private void UpdateDisabledState() {
			if (AnyPawnsMeetSkillRequirement != !disabled) {
				disabled = !AnyPawnsMeetSkillRequirement;
				if (disabled) {
					disabledReason = "Finish_off_skillRequired".Translate(MeleeSkillLevelRequired);
				}
			}
		}
	}
}