using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	/**
	 * Base class for custom designators that deal with selectable Things.
	 */
	public abstract class Designator_SelectableThings : Designator {
		internal readonly ThingDesignatorDef def;

		public override int DraggableDimensions {
			get { return 2; }
		}

		public override bool DragDrawMeasurements {
			get { return true; }
		}

		private bool visible = true;
		public override bool Visible {
			get { return visible; }
		}

		public Designator_SelectableThings(ThingDesignatorDef def) {
			this.def = def;
			defaultLabel = def.label;
			defaultDesc = def.description;
			icon = def.IconTex;
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.DesignateDragStandard;
			soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
			soundSucceeded = def.soundSucceded;
			hotKey = def.hotkeyDef;
		}

		public void SetVisible(bool value) {
			visible = value;
		}

		// this is called by the vanilla DesignationDragger. We are using UnlimitedDesignationDragger instead.
		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			return false;
		}

		// tool selected
		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			AllowToolController.Instance.Dragger.BeginListening(ThingIsRelevant, def.DragHighlightTex);
		}

		public override void DesignateSingleCell(IntVec3 loc) {
			if (ProcessCell(loc) > 0) {
				FinalizeDesignationSucceeded();
			} else {
				FinalizeDesignationFailed();
			}
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			var hitCount = 0;
			foreach (var cell in AllowToolController.Instance.Dragger.GetAffectedCells()) {
				var hits = ProcessCell(cell);
				hitCount += hits;
			}
			if (hitCount > 0) {
				if (def.messageSuccess != null) Messages.Message(def.messageSuccess.Translate(hitCount.ToString()), MessageSound.Silent);
				FinalizeDesignationSucceeded();
			} else {
				if (def.messageFailure != null) Messages.Message(def.messageFailure.Translate(), MessageSound.RejectInput);
			}
		}

		protected abstract bool ThingIsRelevant(Thing item);

		protected abstract int ProcessCell(IntVec3 cell);

		public virtual void SelectedOnGUI() {
		}

	}
}
