namespace AllowTool.Context {
	/// <summary>
	/// Allows the designator to be "picked up" when shift-clicked as a reverse designator.
	/// </summary>
	public interface IReversePickableDesignator {
		bool ReversePickingAllowed { get; }
	}
}