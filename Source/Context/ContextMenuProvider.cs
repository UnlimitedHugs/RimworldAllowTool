using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Settings;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Used by the <see cref="DesignatorContextMenuController"/> to 
	/// associate multiple context menu entries to a designator type.
	/// </summary>
	public struct ContextMenuProvider {
		public Type HandledDesignatorType { get; }
		private readonly BaseContextMenuEntry[] entries;

		public bool HasCustomEnabledEntries {
			get {
				for (var i = 0; i < entries.Length; i++) {
					if(entries[i].Enabled) return true;
				}
				return false;
			}
		}

		public ContextMenuProvider(Type handledType, params BaseContextMenuEntry[] entries) {
			HandledDesignatorType = handledType;
			this.entries = entries;
		}

		public void OpenContextMenu(Designator designator) {
			var menuOptions = entries.Where(e => e.Enabled)
				.Select(e => e.MakeMenuOption(designator))
				.Concat(designator.RightClickFloatMenuOptions).ToList();
			if (menuOptions.Count > 0) {
				Find.WindowStack.Add(new FloatMenu(menuOptions));
			}
		}

		public bool TryInvokeHotkeyAction(Designator designator) {
			// stock right click menu options will not be activated by hotkey
			var firstEnabledEntry = entries.FirstOrDefault(e => e.Enabled);
			if (firstEnabledEntry != null) {
				firstEnabledEntry.ActivateAndHandleResult(designator);
				return true;
			}
			return false;
		}

		public IEnumerable<SettingHandle<bool>> RegisterEntryHandles(ModSettingsPack pack) {
			return entries.Select(e => e.RegisterSettingHandle(pack));
		}
	}
}