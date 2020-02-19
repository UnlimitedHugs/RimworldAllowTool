using Verse;

namespace AllowTool.Settings {
	/// <summary>
	/// Stores settings for the Strip Mine designator that unique for each save file
	/// </summary>
	public class StripMineWorldSettings : IExposable, IConfigurableStripMineSettings {
		private const int DefaultSpacingX = 5;
		private const int DefaultSpacingY = 5;

		private int hSpacing = DefaultSpacingX;
		public int HorizontalSpacing {
			get { return hSpacing; }
			set { hSpacing = value; }
		}

		private int vSpacing = DefaultSpacingY;
		public int VerticalSpacing {
			get { return vSpacing; }
			set { vSpacing = value; }
		}

		private bool variableGridOffset = true;
		public bool VariableGridOffset {
			get { return variableGridOffset; }
			set { variableGridOffset = value; }
		}

		private bool showWindow = true;
		public bool ShowWindow {
			get { return showWindow; }
			set { showWindow = value; }
		}

		private IntVec2 lastGridOffset;
		public IntVec2 LastGridOffset {
			get { return lastGridOffset; }
			set { lastGridOffset = value; }
		}

		public void ExposeData() {
			Scribe_Values.Look(ref hSpacing, "hSpacing", DefaultSpacingX);
			Scribe_Values.Look(ref vSpacing, "vSpacing", DefaultSpacingY);
			Scribe_Values.Look(ref variableGridOffset, "variableOffset", true);
			Scribe_Values.Look(ref showWindow, "showWindow", true);
			Scribe_Values.Look(ref lastGridOffset, "lastOffset");
		}

		public StripMineWorldSettings Clone() {
			return (StripMineWorldSettings)MemberwiseClone();
		}
	}
}