using Verse;

namespace AllowTool.Settings {
	/// <summary>
	/// Stores settings for the Strip Mine designator that unique for each save file
	/// </summary>
	public class StripMineWorldSettings : IExposable, IStripMineSettings {
		private int hSpacing = 5;
		public int HorizontalSpacing {
			get { return hSpacing; }
			set { hSpacing = value; }
		}

		private int vSpacing = 5;
		public int VerticalSpacing {
			get { return vSpacing; }
			set { vSpacing = value; }
		}

		public void ExposeData() {
			Scribe_Values.Look(ref hSpacing, "hSpacing");
			Scribe_Values.Look(ref vSpacing, "vSpacing");
		}

		public StripMineWorldSettings Clone() {
			return (StripMineWorldSettings)MemberwiseClone();
		}
	}
}