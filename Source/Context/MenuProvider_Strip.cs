using System;
using RimWorld;
using Verse;

namespace AllowTool.Context {
	public class MenuProvider_Strip : BaseDesignatorMenuProvider {
		public override string EntryTextKey {
			get { return "Designator_context_strip"; }
		}

		public override string SettingId {
			get { return "providerStrip"; }
		}

		public override Type HandledDesignatorType {
			get { return typeof (Designator_Strip); }
		}

		protected override ThingRequestGroup DesingatorRequestGroup {
			get { return ThingRequestGroup.Everything; }
		}

		public override void ContextMenuAction(Designator designator, Map map) {
			int hitCount = 0;
			var playerFaction = Faction.OfPlayer;
			foreach (var thing in map.listerThings.ThingsInGroup(DesingatorRequestGroup)) {
				if (thing.Faction != playerFaction && designator.CanDesignateThing(thing).Accepted) {
					designator.DesignateThing(thing);
					hitCount++;
				}
			}
			ReportActionResult(hitCount);
		}
	}
}