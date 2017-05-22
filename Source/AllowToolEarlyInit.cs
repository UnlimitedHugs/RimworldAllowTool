using Harmony;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Get out patches in early on, so that we can get our foot in before implied defs are generated
	/// </summary>
	public class AllowToolEarlyInit : Mod {
		public AllowToolEarlyInit(ModContentPack content) : base(content) {
			AllowToolController.HarmonyInstance = HarmonyInstance.Create(AllowToolController.HarmonyInstanceId);
			AllowToolController.HarmonyInstance.PatchAll(typeof(AllowToolController).Assembly);
		}
	}
}