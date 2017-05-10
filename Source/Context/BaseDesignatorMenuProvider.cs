using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Base class for all types that provide context menu functionality on desingators.
	/// Types are instantiated automatically by DesignatorContextMenuController.
	/// </summary>
	public abstract class BaseDesignatorMenuProvider {
		protected delegate void MenuActionMethod(Designator designator, Map map);

		private const string SuccessMessageStringIdSuffix = "_succ";
		private const string FailureMessageStringIdSuffix = "_fail";

		// the text key for the context menu entry, as well as the base for the sucess/failure message keys
		protected abstract string EntryTextKey { get; }
		// the type of the designator this provider should make a context menu for
		public abstract Type HandledDesignatorType { get; }
		// the group of things handled by the designator this handler belongs to
		protected abstract ThingRequestGroup DesingatorRequestGroup { get; }

		public virtual void OpenContextMenu(Designator designator) {
			Find.WindowStack.Add(new FloatMenu(ListMenuEntries(designator).ToList()));
		}

		protected virtual IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, EntryTextKey, ContextMenuAction);
		}

		public virtual void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}

		public virtual void ReportActionResult(int designationCount, string baseMessageKey = null) {
			if (baseMessageKey == null) {
				baseMessageKey = EntryTextKey;
			}
			if (designationCount > 0) {
				Messages.Message((baseMessageKey + SuccessMessageStringIdSuffix).Translate(designationCount), MessageSound.Benefit);
			} else {
				Messages.Message((baseMessageKey + FailureMessageStringIdSuffix).Translate(), MessageSound.RejectInput);
			}
		}

		protected virtual FloatMenuOption MakeMenuOption(Designator designator, string labelKey, MenuActionMethod action, bool includePrefix = true) {
			var label = includePrefix ? "Designator_context_prefix".Translate(labelKey.Translate()) : labelKey.Translate();
			return new FloatMenuOption(label, () => {
				InvokeActionWithErrorHandling(action, designator);
			});
		}

		protected void InvokeActionWithErrorHandling(MenuActionMethod action, Designator designator) {
			try {
				var map = Find.VisibleMap;
				action(designator, map);
			} catch (Exception e) {
				AllowToolController.Instance.Logger.Error("Exception while processing context menu action: " + e);
			}
		}
	}
}