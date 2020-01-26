namespace AllowTool {
	/// <summary>
	/// Designator icon textures are loaded after designator instantiation (textures must be loaded in main thread)
	/// This call ensures that our designators receive their icons at static constructor initialization time.
	/// Icons are not actually needed until a game is loaded, so the delay is no issue.
	/// </summary>
	public interface IDelayedIconResolver {
		void ResolveIcon();
	}
}