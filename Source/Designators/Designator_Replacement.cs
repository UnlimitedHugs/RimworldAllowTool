using System.Collections.Generic;
using HarmonyLib;
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

		public override AcceptanceReport CanDesignateThing(Thing t) {
			return replacedDesignator.CanDesignateThing(t);
		}

		public override void DesignateThing(Thing t) {
			replacedDesignator.DesignateThing(t);
		}
		
		public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions {
			get { return replacedDesignator.RightClickFloatMenuOptions; }
		}

		protected void SetReplacedDesignator(Designator des) {
			// acquire config values from the replaced designator.
			// These are overridden later by values from the def, if it has the appropriate field defined
			replacedDesignator = des;
			defaultLabel = des.defaultLabel;
			defaultDesc = des.defaultDesc;
			hotKey = des.hotKey;
			var reflect = Traverse.Create(des);
			soundSucceeded = reflect.Field(nameof(soundSucceeded)).GetValue<SoundDef>();
			hasDesignateAllFloatMenuOption = reflect.Field(nameof(hasDesignateAllFloatMenuOption)).GetValue<bool>();
			designateAllLabel = reflect.Field(nameof(designateAllLabel)).GetValue<string>();
		}

		protected override void ResolveIcon() {
			if (InheritReplacedDesignatorIcon) {
				icon = replacedDesignator.icon;
			} else {
				base.ResolveIcon();
			}
		}

		protected override void ResolveDragHighlight() {
			HighlightMaterial = DesignatorUtility.DragHighlightThingMat;
		}
	}
}