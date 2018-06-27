using System.Collections.Generic;
using HugsLib.Utils;
using Verse;

namespace AllowTool.Settings {
	/// <summary>
	/// Store settings for a specific world save file
	/// </summary>
	public class WorldSettings : UtilityWorldObject {
		private HashSet<int> partyHuntingPawns = new HashSet<int>();

		public override void ExposeData() {
			base.ExposeData();
			// convert to list for serialization
			var partyHuntingList = new List<int>(partyHuntingPawns);
			Scribe_Collections.Look(ref partyHuntingList, "partyHuntingPawns");
			partyHuntingPawns = new HashSet<int>(partyHuntingList);
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