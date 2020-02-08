using System;
using HugsLib.Utils;
using UnityEngine;
using Verse;

namespace AllowTool {
	public class Dialog_StripMineSettings : Window {
		private const int SpacingMinValue = 1;
		private const int SpacingMaxValue = 50;
		private const float RowHeight = 36f;
		private const float Spacing = 4f;
		private const float LabelColumnWidthPercent = .666f;

		public event Action<IStripMineSettings> SettingsChanged;
		public event Action CloseAccept;
		public event Action CloseCancel;

		private readonly IStripMineSettings settings;
		
		private bool showWindowValue;
		public bool ShowWindowToggleValue {
			get { return showWindowValue; }
			set { showWindowValue = value; }
		}

		public Vector2 WindowPosition {
			get { return new Vector2(windowRect.x, windowRect.y); }
			set {
				windowRect.x = value.x;
				windowRect.y = value.y;
			}
		}

		public override Vector2 InitialSize {
			get { return new Vector2(320f, Margin * 2 + RowHeight * 4 + Spacing * 4); }
		}

		public Dialog_StripMineSettings(IStripMineSettings settings) {
			this.settings = settings;
			draggable = true;
			focusWhenOpened = false;
			forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
			preventCameraMotion = false;
			layer = WindowLayer.SubSuper;
		}

		protected override void SetInitialSizeAndPosition() {
			base.SetInitialSizeAndPosition();
			windowRect.x = windowRect.y = 0;
		}

		public override void DoWindowContents(Rect inRect) {
			var listing = new Listing_Standard { maxOneColumn = true };
			listing.Begin(inRect);
			var originalAnchor = Text.Anchor;
			Text.Anchor = TextAnchor.MiddleLeft;
			settings.HorizontalSpacing = DoIntSpinner("StripMine_win_horizontalSpacing".Translate(), settings.HorizontalSpacing, listing, out bool horizontalChanged);
			listing.Gap(Spacing);
			settings.VerticalSpacing = DoIntSpinner("StripMine_win_verticalSpacing".Translate(), settings.VerticalSpacing, listing, out bool verticalChanged);
			listing.Gap(Spacing);
			DoCustomCheckbox(listing);
			listing.Gap(Spacing * 2);
			var buttonsRect = listing.GetRect(RowHeight);
			var cancelBtnRect = buttonsRect.LeftPart(1f - LabelColumnWidthPercent);
			if (Widgets.ButtonText(cancelBtnRect, "CancelButton".Translate())) {
				Cancel();
			}
			var confirmBtnRect = buttonsRect.RightPartPixels(buttonsRect.width - cancelBtnRect.width - Spacing);
			GUI.color = Color.green;
			if (Widgets.ButtonText(confirmBtnRect, "AcceptButton".Translate())) {
				Accept();
			}
			GUI.color = Color.white;
			if (horizontalChanged || verticalChanged) {
				SettingsChanged?.Invoke(settings);
			}
			Text.Anchor = originalAnchor;
			listing.End();
			ConfineWindowToScreenArea();
		}

		private void Accept() {
			CloseAccept?.Invoke();
			Close(false);
		}

		private void Cancel() {
			CloseCancel?.Invoke();
			Close(false);
		}

		public override void OnAcceptKeyPressed() {
			Accept();
			Event.current.Use();
		}

		public override void OnCancelKeyPressed() {
			Cancel();
			Event.current.Use();
		}

		private int DoIntSpinner(string label, int value, Listing_Standard listing, out bool changed) {
			void TryChangeValue(int delta, ref bool hasChanged) {
				var newValue = Mathf.Clamp(value + delta * (HugsLibUtility.ShiftIsHeld ? 5 : 1), SpacingMinValue, SpacingMaxValue);
				if (newValue != value) {
					value = newValue;
					hasChanged = true;
				}
			}
			changed = false;
			var rowRect = listing.GetRect(RowHeight);
			if (HugsLibUtility.ShiftIsHeld) {
				AllowToolController.Logger.Trace(rowRect);
			}
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

		private void DoCustomCheckbox(Listing_Standard listing) {
			var checkboxRect = listing.GetRect(RowHeight);
			DoTipArea(checkboxRect, "StripMine_win_showWindow_tip".Translate());
			Widgets.Label(checkboxRect, "StripMine_win_showWindow".Translate());
			const float checkmarkHeight = 24f;
			var checkmarkOffset = new Vector2(
				checkboxRect.x + checkboxRect.width * LabelColumnWidthPercent,
				checkboxRect.y + (checkboxRect.height - checkmarkHeight) / 2f
			);
			Widgets.Checkbox(checkmarkOffset, ref showWindowValue);
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