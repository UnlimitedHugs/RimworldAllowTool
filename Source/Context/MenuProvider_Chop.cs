using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Chop : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_chop"; }
		}

		public override string SettingId {
			get { return "providerChop"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_PlantsHarvestWood); }
		}

		protected override ThingRequestGroup DesignatorRequestGroup {
			get { return ThingRequestGroup.Plant; }
		}

		protected override IEnumerable<FloatMenuOption> ListMenuEntries(Designator designator) {
			yield return MakeMenuOption(designator, "Designator_context_chopFullyGrown", (des, map) =>
					Find.DesignatorManager.Select(new Designator_ChopFullyGrown()),
			"Designator_context_fullyGrown_desc");
		}
	}
}