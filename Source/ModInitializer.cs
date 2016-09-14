using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace AllowTool {
	/**
	 * Entry point.
	 * Creates a GameObject on the scene to forward necessary events to AllowToolController
	 */
	[StaticConstructorOnStartup]
	public class ModInitializer : ITab {

		private static GameObject obj;

		public ModInitializer(){
			if (obj != null) return;
			obj = new GameObject("AllowToolLoader");
			obj.AddComponent<ModInitializerComponent>();
			Object.DontDestroyOnLoad(obj);
		}
		
		protected override void FillTab() {			
		}
	}
}
