using UnityEngine;
using Verse;
using Object = UnityEngine.Object;

namespace AllowTool {
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
