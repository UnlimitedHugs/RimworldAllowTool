using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// A float menu option with the Allow Tool watermark (when enabled in settings) and a description tooltip when hovered over.
	/// </summary>
	public class ATFloatMenuOption : FloatMenuOption {
		private const float WatermarkDrawSize = 30f;
		private const float MouseOverLabelShift = 4f;

		private readonly bool showWatermark;
		private readonly string tooltipText;

		public ATFloatMenuOption(string label, Action action, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null, string tooltipText = null) : 
			base(label, action, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget) {
			this.tooltipText = tooltipText;
			showWatermark = AllowToolController.Instance.Handles.ContextWatermarkSetting;
			if (showWatermark) {
				Label = "      " + label;
			}
		}

		public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu) {
			var result = base.DoGUI(rect, colonistOrdering, floatMenu);
			if (showWatermark) {
				var hoverRect = new Rect(rect.x, rect.y, rect.width, rect.height - 1f);
				bool hovering = !Disabled && Mouse.IsOver(hoverRect);
				var tex = AllowToolDefOf.Textures.contextMenuWatermark;
				GUI.DrawTexture(new Rect(rect.x + (hovering?MouseOverLabelShift:0f), rect.y, WatermarkDrawSize, WatermarkDrawSize), tex);
				if (tooltipText != null) {
					TooltipHandler.TipRegion(hoverRect, tooltipText);
				}
			}
			return result;
		}
	}
}