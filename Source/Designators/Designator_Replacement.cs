using System.Collections.Generic;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Base class for Allow Tool designators that replace a vanilla designator
	/// </summary>
	public abstract class Designator_Replacement : Designator_SelectableThings {
		protected Designator replacedDesignator;

		private bool InheritReplacedDesignatorIcon {
			get { return !AllowToolController.Instance.Handles.ReplaceIconsSetting; }
		}

		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get {
				foreach (var floatMenuOption in base.RightClickFloatMenuOptions) {
					yield return floatMenuOption;
				}
				if (replacedDesignator != null) {
					foreach (var option in replacedDesignator.RightClickFloatMenuOptions) {
						yield return option;
					}
				}
			}
		}

		protected override void ResolveIcon() {
			if (replacedDesignator != null && InheritReplacedDesignatorIcon) {
				icon = replacedDesignator.icon;
			} else {
				base.ResolveIcon();
			}
		}
	}
}