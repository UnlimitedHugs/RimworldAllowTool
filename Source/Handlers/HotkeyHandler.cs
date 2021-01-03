using System.Collections.Generic;
using System.Linq;
using AllowTool.Context;
using UnityEngine;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Handles global key press events for summoning Allow Tool designators
	/// </summary>
	public class HotKeyHandler {
		private readonly List<HotkeyListener> activeListeners = new List<HotkeyListener>();

		public void OnGUI() {
			if (Event.current.type == EventType.KeyDown) {
				CheckForHotkeyPresses();
			}
		}

		public void RebindAllDesignators() {
			activeListeners.Clear();
			var providers = AllowToolUtility.EnumerateResolvedDirectDesignators()
				.Where(d => d is IGlobalHotKeyProvider p && p.GlobalHotKey != null);
			foreach (var designator in providers) {
				activeListeners.Add(new HotkeyListener(designator, ((IGlobalHotKeyProvider)designator).GlobalHotKey));
			}
		}

		private void CheckForHotkeyPresses() {
			if (Find.CurrentMap == null || Event.current.keyCode == KeyCode.None) return;
			if (AllowToolDefOf.ToolContextMenuAction.JustPressed) {
				DesignatorContextMenuController.ProcessContextActionHotkeyPress();
			}
			if (!AllowToolController.Instance.Handles.GlobalHotkeysSetting) return;
			for (int i = 0; i < activeListeners.Count; i++) {
				if (activeListeners[i].hotKey.JustPressed && activeListeners[i].designator.Visible) {
					Find.DesignatorManager.Select(activeListeners[i].designator);
					break;
				}
			}
		}

		private struct HotkeyListener {
			public readonly Designator designator;
			public readonly KeyBindingDef hotKey;
			public HotkeyListener(Designator designator, KeyBindingDef hotKey) {
				this.designator = designator;
				this.hotKey = hotKey;
			}
		}
	}
}