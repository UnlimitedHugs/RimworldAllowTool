using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// A toggle button with context options to switch party hunting mode for a pawn.
	/// </summary>
	public class Command_PartyHunt : Command_Toggle {
		private static readonly Vector2 overlayIconOffset = new Vector2(59f, 57f);

		private readonly Pawn pawn;

		public Command_PartyHunt(Pawn pawn) {
			this.pawn = pawn;
			icon = AllowToolDefOf.Textures.partyHunt;
			defaultLabel = "PartyHuntToggle_label".Translate();
			defaultDesc = "PartyHuntToggle_desc".Translate();
			isActive = () => AllowToolUtility.PartyHuntIsEnabled(pawn);
			toggleAction = ToggleAction;
			hotKey = KeyBindingDefOf.Misc9;
			disabled = !AllowToolUtility.PawnCapableOfViolence(pawn);
			if (disabled) {
				disabledReason = "IsIncapableOfViolence".Translate(pawn.Name.ToStringShort, pawn);
			}
		}

		private void ToggleAction() {
			AllowToolController.Instance.WorldSettings.TogglePawnPartyHunting(pawn, !AllowToolUtility.PartyHuntIsEnabled(pawn));
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth) {
			var result = base.GizmoOnGUI(topLeft, maxWidth);
			if (Event.current.type == EventType.Repaint) {
				AllowToolUtility.DrawRightClickIcon(topLeft.x + overlayIconOffset.x, topLeft.y + overlayIconOffset.y);
			}
			return result;
		}

		public override bool InheritFloatMenuInteractionsFrom(Gizmo other) {
			// activate context menu items only for one selected pawn
			return false;
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get {
				yield return AllowToolUtility.MakeSettingCheckmarkOption("setting_partyHuntFinish_label", null, AllowToolController.Instance.PartyHuntFinishSetting);
				yield return AllowToolUtility.MakeSettingCheckmarkOption("setting_partyHuntDesignated_label", null, AllowToolController.Instance.PartyHuntDesignatedSetting);
			}
		}
	}
}