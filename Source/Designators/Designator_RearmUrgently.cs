using HugsLib.Utils;
using RimWorld;
using Verse;

namespace AllowTool {
	public class Designator_RearmUrgently : Designator_RearmTrap {
		public Designator_RearmUrgently() {
			icon = AllowToolDefOf.Textures.rearmUrgently;
		}

		protected override DesignationDef Designation {
			get { return AllowToolDefOf.RearmUrgentlyDesignation; }
		}

		public override void DrawMouseAttachments() {
			base.DrawMouseAttachments();
			AllowToolUtility.DrawMouseAttachedLabel("RearmUrgently_cursorTip".Translate());
		}

		public override void DesignateThing(Thing t) {
			base.DesignateThing(t);
			t.ToggleDesignation(DesignationDefOf.RearmTrap, false);
		}
	}
}