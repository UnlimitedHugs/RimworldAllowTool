namespace AllowTool {
	public interface IConfigurableStripMineSettings {
		int HorizontalSpacing { get; set; }
		int VerticalSpacing { get; set; }
		bool VariableGridOffset { get; set; }
		bool ShowWindow { get; set; }
	}
}