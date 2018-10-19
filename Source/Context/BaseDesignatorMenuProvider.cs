using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;
// ReSharper disable VirtualMemberNeverOverridden.Global

namespace AllowTool.Context {
	/// <summary>
	/// Base class for all types that provide context menu functionality on designators.
	/// Types are instantiated automatically by DesignatorContextMenuController.
	/// </summary>
	public abstract class BaseDesignatorMenuProvider {
		protected delegate void MenuActionMethod(Designator designator, Map map);

		protected const string SuccessMessageStringIdSuffix = "_succ";
		protected const string FailureMessageStringIdSuffix = "_fail";

		// the toggle for this provider, assigned automatically by AllotToolController
		public virtual SettingHandle<bool> ProviderHandle { get; set; } 
		// the text key for the context menu entry, as well as the base for the success/failure message keys
		public abstract string EntryTextKey { get; }
		// the type of the designator this provider should make a context menu for
		public abstract Type HandledDesignatorType { get; }
		// the group of things handled by the designator this handler belongs to
		protected virtual ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.Everything; }
		}
		// returning false will disable the context menu and the overlay icon
		public virtual bool Enabled {
			get {
				var handle = ProviderHandle;
				if(handle != null) return handle.Value;
				return true;
			}
		}
		// id for the setting handle creation. Null disables the handle
		public virtual string SettingId {
			get { return null; }
		}

		public virtual void OpenContextMenu(Designator designator) {
			var handlerEntries = Enabled ? ListMenuEntries(designator) : new FloatMenuOption[0];
			var allEntries = handlerEntries.Concat(designator.RightClickFloatMenuOptions).ToList();
			if (allEntries.Count > 0) {
				Find.WindowStack.Add(new FloatMenu(allEntries));
			}
		}

		protected virtual IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, EntryTextKey, ContextMenuAction);
		}

		public virtual void ContextMenuAction(Designator designator, Map map) {
			ContextMenuAction(designator, map, null);
		}

		public virtual void ContextMenuAction(Designator designator, Map map, Predicate<Thing> thingFilter) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesignatorRequestGroup)) {
				if (ValidForDesignation(thing) && (thingFilter == null || thingFilter(thing)) && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}

		protected void ContextMenuActionInHomeArea(Designator des, Map map) {
			var homeArea = map.areaManager.Home;
			ContextMenuAction(des, map, thing => homeArea.GetCellBool(map.cellIndices.CellToIndex(thing.Position)));
		}

		// called if the common hotkey is pressed while the designator is selected
		// or the designator is the first reverse designator
		public virtual bool TryInvokeHotkeyAction(Designator designator) {
			if (Enabled) {
				var entry = ListMenuEntries(designator).FirstOrDefault();
				if (entry is ATFloatMenuOption && entry.action != null) {
					entry.action();
					return true;
				}
			}
			return false;
		}

		public virtual void ReportActionResult(int designationCount, string baseMessageKey = null) {
			if (baseMessageKey == null) {
				baseMessageKey = EntryTextKey;
			}
			if (designationCount > 0) {
				Messages.Message((baseMessageKey + SuccessMessageStringIdSuffix).Translate(designationCount), MessageTypeDefOf.TaskCompletion);
			} else {
				Messages.Message((baseMessageKey + FailureMessageStringIdSuffix).Translate(), MessageTypeDefOf.RejectInput);
			}
		}

		protected virtual FloatMenuOption MakeMenuOption(Designator designator, string labelKey, MenuActionMethod action, string descriptionKey = null, Texture2D extraIcon = null) {
			const float extraIconsSize = 24f;
			const float labelMargin = 10f;
			Func<Rect, bool> extraIconOnGUI = null;
			var extraPartWidth = 0f;
			if (extraIcon != null) {
				extraIconOnGUI = rect => {
					Graphics.DrawTexture(new Rect(rect.x + labelMargin, rect.height / 2f - extraIconsSize / 2f + rect.y, extraIconsSize, extraIconsSize), extraIcon);
					return false;
				};
				extraPartWidth = extraIconsSize + labelMargin;
			}
			return new ATFloatMenuOption(labelKey.Translate(), () => {
				InvokeActionWithErrorHandling(action, designator);
			}, MenuOptionPriority.Default, null, null, extraPartWidth, extraIconOnGUI, null, descriptionKey?.Translate());
		}

		protected virtual bool ValidForDesignation(Thing thing) {
			return thing?.def != null && thing.Map != null && !thing.Map.fogGrid.IsFogged(thing.Position);
		}

		protected void InvokeActionWithErrorHandling(MenuActionMethod action, Designator designator) {
			try {
				var map = Find.CurrentMap;
				if(map == null) return;
				action(designator, map);
			} catch (Exception e) {
				AllowToolController.Logger.Error("Exception while processing context menu action: " + e);
			}
		}
	}
}