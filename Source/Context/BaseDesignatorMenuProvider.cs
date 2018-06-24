using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using Verse;
using Verse.Sound;

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
			var entries = ListMenuEntries(designator).Concat(designator.RightClickFloatMenuOptions);
			Find.WindowStack.Add(new FloatMenu(entries.ToList()));
		}

		protected virtual IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, EntryTextKey, ContextMenuAction);
		}

		public virtual void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesignatorRequestGroup)) {
				if (ValidForDesignation(thing) && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}

		// called if the common hotkey is pressed while the designator is selected
		public virtual void HotkeyAction(Designator designator) {
			InvokeActionWithErrorHandling(ContextMenuAction, designator);
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

		protected virtual FloatMenuOption MakeMenuOption(Designator designator, string labelKey, MenuActionMethod action, string descriptionKey = null) {
			return new ATFloatMenuOption(labelKey.Translate(), () => {
				InvokeActionWithErrorHandling(action, designator);
			}, MenuOptionPriority.Default, null, null, 0f, null, null, descriptionKey!=null?descriptionKey.Translate():null);
		}

		protected FloatMenuOption MakeSettingCheckmarkOption(string labelKey, string descriptionKey, SettingHandle<bool> handle) {
			const float checkmarkButtonSize = 24f;
			const float labelMargin = 10f;
			bool checkOn = handle.Value;
			return new ATFloatMenuOption(labelKey.Translate(), () => {
					handle.Value = !handle.Value;
					checkOn = handle.Value;
					HugsLibController.SettingsManager.SaveChanges();
					var feedbackSound = checkOn?SoundDefOf.Checkbox_TurnedOn:SoundDefOf.Checkbox_TurnedOff;
					feedbackSound.PlayOneShotOnCamera();
				}, MenuOptionPriority.Default, null, null, checkmarkButtonSize + labelMargin, rect => {
					Widgets.Checkbox(rect.x + labelMargin, rect.height/2f - checkmarkButtonSize/2f + rect.y, ref checkOn);
					return false;
				}, null, descriptionKey.Translate());
		}

		protected virtual bool ValidForDesignation(Thing thing) {
			return thing != null && thing.def != null && thing.Map != null && !thing.Map.fogGrid.IsFogged(thing.Position);
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