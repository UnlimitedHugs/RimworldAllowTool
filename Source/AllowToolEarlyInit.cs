using System;
using Harmony;
using Verse;

namespace AllowTool {
	/// <summary>
	/// Get our patches applied early on, so that we can get our foot in before implied defs are generated
	/// </summary>
	public class AllowToolEarlyInit : Mod {
		public AllowToolEarlyInit(ModContentPack content) : base(content) {
			try {
				AllowToolController.HarmonyInstance = HarmonyInstance.Create(AllowToolController.HarmonyInstanceId);
				AllowToolController.HarmonyInstance.PatchAll(typeof(AllowToolController).Assembly);
			} catch (Exception e) {
				// Resharper disable once
				Log.Error("AllowTool failed to apply Harmony patches: "+e);
			}
		}
	}
}