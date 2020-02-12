using System;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Dialog_StripMineConfiguration : Window {
		public delegate void ClosingCallback(bool accept);

		private const int SpacingMinValue = 1;
		private const int SpacingMaxValue = 50;
		private const float RowHeight = 36f;
		private const float Spacing = 4f;
		private const float LabelColumnWidthPercent = .666f;

		public event Action<IConfigurableStripMineSettings> SettingsChanged;
		public event ClosingCallback Closing;

		private readonly IConfigurableStripMineSettings settings;
		
		public Vector2 WindowPosition {
			get { return new Vector2(windowRect.x, windowRect.y); }
			set {
				windowRect.x = value.x;
				windowRect.y = value.y;
			}
		}

		public override Vector2 InitialSize {
			get { return new Vector2(320f, Margin * 2 + RowHeight * 5 + Spacing * 5); }
		}

		public Dialog_StripMineConfiguration(IConfigurableStripMineSettings settings) {
			this.settings = settings;
			draggable = true;
			focusWhenOpened = false;
			forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
			preventCameraMotion = false;
			layer = WindowLayer.SubSuper;
		}

		protected override void SetInitialSizeAndPosition() {
			var presetPosition = WindowPosition;
			base.SetInitialSizeAndPosition();
			windowRect.x = presetPosition.x;
			windowRect.y = presetPosition.y;
		}

		public override void DoWindowContents(Rect inRect) {
			var listing = new Listing_Standard { maxOneColumn = true };
			listing.Begin(inRect);
			var originalAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			var settingsChanged = false;
			
			settings.HorizontalSpacing = DoIntSpinner("StripMine_win_horizontalSpacing".Translate(), settings.HorizontalSpacing, listing, ref settingsChanged);
			listing.Gap(Spacing);
			
			settings.VerticalSpacing = DoIntSpinner("StripMine_win_verticalSpacing".Translate(), settings.VerticalSpacing, listing, ref settingsChanged);
			listing.Gap(Spacing);
			
			settings.VariableGridOffset = DoCustomCheckbox("StripMine_win_variableOffset", "StripMine_win_variableOffset_tip", 
				settings.VariableGridOffset, listing, ref settingsChanged);
			listing.Gap(Spacing);

			settings.ShowWindow = DoCustomCheckbox("StripMine_win_showWindow", "StripMine_win_showWindow_tip", 
				settings.ShowWindow, listing, ref settingsChanged);
			listing.Gap(Spacing * 2);
			
			var buttonsRect = listing.GetRect(RowHeight);
			var cancelBtnRect = buttonsRect.LeftPart(1f - LabelColumnWidthPercent);
			if (Widgets.ButtonText(cancelBtnRect, "CancelButton".Translate())) {
				CancelAndClose();
			}
			var confirmBtnRect = buttonsRect.RightPartPixels(buttonsRect.width - cancelBtnRect.width - Spacing);
			GUI.color = Color.green;
			if (Widgets.ButtonText(confirmBtnRect, "AcceptButton".Translate())) {
				AcceptAndClose();
			}
			GUI.color = Color.white;
			if (settingsChanged) {
				SettingsChanged?.Invoke(settings);
			}
			Text.Anchor = originalAnchor;
			listing.End();
			ConfineWindowToScreenArea();
		}

		public void CancelAndClose() {
			Closing?.Invoke(false);
			Close(false);
		}

		private void AcceptAndClose() {
			Closing?.Invoke(true);
			Close(false);
		}

		public override void OnAcceptKeyPressed() {
			AcceptAndClose();
			Event.current.Use();
		}

		public override void OnCancelKeyPressed() {
			CancelAndClose();
			Event.current.Use();
		}

		private int DoIntSpinner(string label, int value, Listing_Standard listing, ref bool changed) {
			void TryChangeValue(int delta, ref bool hasChanged) {
				var newValue = Mathf.Clamp(value + delta * (HugsLibUtility.ShiftIsHeld ? 5 : 1), SpacingMinValue, SpacingMaxValue);
				if (newValue != value) {
					value = newValue;
					hasChanged = true;
				}
			}
			var rowRect = listing.GetRect(RowHeight);
			if (DoTipArea(rowRect)) {
				if (Event.current.isScrollWheel) {
					var delta = Event.current.delta.y < 0 ? 1 : -1;
					TryChangeValue(delta, ref changed);
					Event.current.Use();
				}
			}
			var originalAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			var labelRect = rowRect.LeftPart(LabelColumnWidthPercent);
			Widgets.Label(labelRect, label);
			var spinnerRect = rowRect.RightPartPixels(rowRect.width - labelRect.width);
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(spinnerRect, value.ToString());
			if (Widgets.ButtonText(spinnerRect.LeftPart(.333f), "-")) {
				TryChangeValue(-1, ref changed);
			}
			if (Widgets.ButtonText(spinnerRect.RightPart(.333f), "+")) {
				TryChangeValue(1, ref changed);
			}
			Text.Anchor = originalAnchor;
			return value;
		}

		private bool DoCustomCheckbox(string labelKey, string tooltipKey, bool value, Listing_Standard listing, ref bool changed) {
			var checkboxRect = listing.GetRect(RowHeight);
			DoTipArea(checkboxRect, tooltipKey.Translate());
			Widgets.Label(checkboxRect, labelKey.Translate());
			const float checkmarkHeight = 24f;
			var checkmarkOffset = new Vector2(
				checkboxRect.x + checkboxRect.width * LabelColumnWidthPercent,
				checkboxRect.y + (checkboxRect.height - checkmarkHeight) / 2f
			);
			var originalValue = value;
			Widgets.Checkbox(checkmarkOffset, ref value);
			if (value != originalValue) {
				changed = true;
			}
			return value;
		}

		private bool DoTipArea(Rect rect, string tooltip = null) {
			if (Mouse.IsOver(rect)) {
				Widgets.DrawHighlight(rect);
				if (tooltip != null) {
					TooltipHandler.TipRegion(rect, tooltip);
				}
				return true;
			}
			return false;
		}

		private void ConfineWindowToScreenArea() {
			if (windowRect.x < 0) windowRect.x = 0;
			if (windowRect.y < 0) windowRect.y = 0;
			if (windowRect.xMax > UI.screenWidth) windowRect.x = UI.screenWidth - windowRect.width;
			if (windowRect.yMax > UI.screenHeight) windowRect.y = UI.screenHeight - windowRect.height;
		}
	}
}