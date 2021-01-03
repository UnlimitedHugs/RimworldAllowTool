using System.Collections.Generic;
using Verse;

// ReSharper disable once CheckNamespace  TODO: Move to AllowTool.Settings on next major update
namespace AllowTool {
	public class PartyHuntSettings : IExposable {
		private HashSet<int> partyHuntingPawns = new HashSet<int>();

		private bool autoFinishOff = true;
		public bool AutoFinishOff {
			get { return autoFinishOff; }
			set { autoFinishOff = value; }
		}

		private bool huntDesignatedOnly;	
		public bool HuntDesignatedOnly {
			get { return huntDesignatedOnly; }
			set { huntDesignatedOnly = value; }
		}

		private bool unforbidDrops;	
		public bool UnforbidDrops {
			get { return unforbidDrops; }
			set { unforbidDrops = value; }
		}

		public void ExposeData() {
			// convert to list for serialization
			var partyHuntingList = new List<int>(partyHuntingPawns);
			Scribe_Collections.Look(ref partyHuntingList, "pawns");
			partyHuntingPawns = new HashSet<int>(partyHuntingList);

			Scribe_Values.Look(ref autoFinishOff, "finishOff", true);
			Scribe_Values.Look(ref huntDesignatedOnly, "designatedOnly");
			Scribe_Values.Look(ref unforbidDrops, "unforbid");
		}

		public bool PawnIsPartyHunting(Pawn pawn) {
			return partyHuntingPawns.Contains(pawn.thingIDNumber);
		}

		public void TogglePawnPartyHunting(Pawn pawn, bool enable) {
			var id = pawn.thingIDNumber;
			if (enable) {
				partyHuntingPawns.Add(id);
			} else {
				partyHuntingPawns.Remove(id);
			}
		}
	}
}