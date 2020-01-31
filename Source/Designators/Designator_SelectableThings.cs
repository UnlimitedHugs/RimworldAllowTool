using System.Collections.Generic;
using AllowTool.Context;
using RimWorld;
using UnityEngine;
using Verse;

namespace AllowTool {
	
	/// <summary>
	/// Base class for custom designators that deal with selectable Things.
	/// This mainly exists to allow the use of an alternative DesignationDragger.
	/// </summary>
	public abstract class Designator_SelectableThings : Designator, IReversePickableDesignator {
		public ThingDesignatorDef Def { get; private set; }
		protected int numThingsDesignated;
		private Texture2D dragHighlight;

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

		public virtual bool ReversePickingAllowed {
			get { return true; }
		}

		protected Designator_SelectableThings() {
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
		}

		protected void UseDesignatorDef(ThingDesignatorDef def) {
			Def = def;
			defaultLabel = def.label;
			defaultDesc = def.description;
			soundSucceeded = def.soundSucceeded;
			hotKey = def.hotkeyDef;
			visible = AllowToolController.Instance.IsDesignatorEnabledInSettings(def);
			def.GetDragHighlightexture(tex => dragHighlight = tex);
			ResolveIcon();
		}

		protected virtual void ResolveIcon() {
			Def.GetIconTexture(tex => icon = tex);
		}

		// this is called by the vanilla DesignationDragger. We are using UnlimitedDesignationDragger instead.
		// returning false prevents the vanilla dragger from selecting any of the cells.
		public override AcceptanceReport CanDesignateCell(IntVec3 c) {
			return false;
		}
		
		public override void Selected() {
			AllowToolController.Instance.Dragger.BeginListening(CanDesignateThing, dragHighlight);
		}

		public override void DesignateSingleCell(IntVec3 cell) {
			numThingsDesignated = 0;
			var map = Find.CurrentMap;
			if (map == null) return;
			var things = map.thingGrid.ThingsListAt(cell);
			for (int i = 0; i < things.Count; i++) {
				var t = things[i];
				if (CanDesignateThing(t).Accepted) {
					DesignateThing(t);
					numThingsDesignated++;
				}
			}
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			var hitCount = 0;
			foreach (var cell in AllowToolController.Instance.Dragger.GetAffectedCells()) {
				DesignateSingleCell(cell);
				hitCount += numThingsDesignated;
			}
			if (hitCount > 0) {
				if (Def.messageSuccess != null) Messages.Message(Def.messageSuccess.Translate(hitCount.ToString()), MessageTypeDefOf.SilentInput);
				FinalizeDesignationSucceeded();
			} else {
				if (Def.messageFailure != null) Messages.Message(Def.messageFailure.Translate(), MessageTypeDefOf.RejectInput);
				FinalizeDesignationFailed();
			}
		}
	}
}
