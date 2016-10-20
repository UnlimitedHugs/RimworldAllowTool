using UnityEngine;

namespace AllowTool {
	public class AllowToolUtility {
		public static bool ShiftIsHeld {
			get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
		}

		public static bool ControlIsHeld {
			get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); }
		}
	}
}