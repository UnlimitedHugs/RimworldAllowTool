using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	/// <summary>
	/// Adds options for urgent rearming of traps.
	/// First entry activates a designator, second option marks all for urgent rearming
	/// </summary>
	public class MenuProvider_Rearm : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_rearmAll"; }
		}

		public override string SettingId {
			get { return "providerRearm"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof(Designator_RearmTrap); }
		}

		protected override ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.BuildingArtificial; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			var urgentDesignator = new Designator_RearmUrgently();
			yield return MakeMenuOption(urgentDesignator, "Designator_context_rearm", (des, map) => Find.DesignatorManager.Select(des), null, AllowToolDefOf.Textures.rearmUrgently);
			yield return MakeMenuOption(urgentDesignator, EntryTextKey, ContextMenuAction, null, AllowToolDefOf.Textures.rearmUrgently);
		}
	}
}