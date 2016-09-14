using UnityEngine;

namespace AllowTool {
	/**
	 * Attached to the scene object. Forwards Unity events to the controller.
	 */
	public class ModInitializerComponent : MonoBehaviour {
		public void Update() {
			AllowToolController.Instance.Update();
		}

		public void OnGUI() {
			AllowToolController.Instance.OnGUI();
		}

		public void OnLevelWasLoaded(int level) {
			AllowToolController.Instance.OnLevelLoaded(level);
		}
	}
}
