using HugsLib.Utils;
using Verse;

namespace AllowTool.Settings {
	/// <summary>
	/// Store settings for a specific world save file
	/// </summary>
	public class WorldSettings : UtilityWorldObject {
		private StripMineWorldSettings stripMine;
		public StripMineWorldSettings StripMine {
			get { return stripMine ?? (stripMine = new StripMineWorldSettings()); }
			set { stripMine = value; }
		}

		private PartyHuntSettings partyHunt;
		public PartyHuntSettings PartyHunt {
			get { return partyHunt ?? (partyHunt = new PartyHuntSettings()); }
			set { partyHunt = value; }
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Deep.Look(ref stripMine, "stripMine");
			Scribe_Deep.Look(ref partyHunt, "partyHunt");
		}
	}
}