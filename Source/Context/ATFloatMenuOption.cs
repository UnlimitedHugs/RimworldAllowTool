using System;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace AllowTool.Context {
	public class ATFloatMenuOption : FloatMenuOption {
		private const float WatermarkDrawSize = 30f;
		private const float MouseOverLabelShift = 4f;

		private readonly bool showWatermark;

		public ATFloatMenuOption(string label, Action action, MenuOptionPriority priority = MenuOptionPriority.Default, Action mouseoverGuiAction = null, Thing revalidateClickTarget = null, float extraPartWidth = 0, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null) : base(label, action, priority, mouseoverGuiAction, revalidateClickTarget, extraPartWidth, extraPartOnGUI, revalidateWorldClickTarget) {
			showWatermark = AllowToolController.Instance.ContextWatermarkSetting;
			if (showWatermark) {
				Label = "      " + label;
			}
		}

		public override bool DoGUI(Rect rect, bool colonistOrdering) {
			var result = base.DoGUI(rect, colonistOrdering);
			if (showWatermark) {
				var hoverRect = new Rect(rect.x, rect.y, rect.width, rect.height - 1f);
				bool hovering = !Disabled && Mouse.IsOver(hoverRect);
				var tex = AllowToolDefOf.Textures.contextMenuWatermark;
				GUI.DrawTexture(new Rect(rect.x + (hovering?MouseOverLabelShift:0f), rect.y, WatermarkDrawSize, WatermarkDrawSize), tex);
			}
			return result;
		}
	}
}